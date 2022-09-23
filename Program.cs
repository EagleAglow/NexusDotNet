using SocketIOClient;
using SocketIOClient.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus
{
        public class qStruct
    {
        public string tag { get; set; }
        public string content { get; set; }
    }

    internal class Program
    {
        // queues for messages between SocketIO server and NamedPipe

        //        ConcurrentQueue<qStruct> qToPipe;
        //        ConcurrentQueue<qStruct> qToServer;
        static public qStruct pipeItem;
        static public qStruct serverItem;

        static public SocketIO socket;
        static public NamedPipeServerStream pipeStreamIn;
        static public NamedPipeServerStream pipeStreamOut;
        static string receivedText;

        static void Main(string[] args)  // try reading command line??
        {

            // setup ConcurrentQueues for message traffic between NamedPipe and SocketIO server.
            ConcurrentQueue<qStruct> qToPipe = new ConcurrentQueue<qStruct>();
            ConcurrentQueue<qStruct> qToServer = new ConcurrentQueue<qStruct>();

            pipeItem = new qStruct();
            pipeItem.tag = "x";
            pipeItem.content = "z";
            //            qToPipe.Enqueue(pipeItem);
            //            qToServer.Enqueue(pipeItem);

            var serverURL = "";
            var pipeName = "";     // Base pipe name

            // look for two command line arguments (either one first)
            if (args.Length == 2)
            {
                if (args[0].StartsWith("/pipe="))
                {
                    pipeName = args[0].Substring(6);
                }
                if (args[1].StartsWith("/pipe="))
                {
                    pipeName = args[1].Substring(6);
                }
                if (args[0].StartsWith("/server="))
                {
                    serverURL = args[0].Substring(8);
                }
                if (args[1].StartsWith("/server="))
                {
                    serverURL = args[1].Substring(8);
                }
            }
            else
            {
                Console.WriteLine("This program needs two command line arguments, like:");
                Console.WriteLine("Nexus.exe /pipe=nexus /server=http://192.168.1.12:3000");
                Console.WriteLine("(Do not use spaces within pipe name or server URL)");
                Console.WriteLine("Press Enter key to close program...");
                Console.ReadLine();
                Environment.Exit(0); // bail out
            }

            var pipeNameIn = pipeName + "In";     // In and Out refer to use in the VBA program, so
            var pipeNameOut = pipeName + "Out";   // VBA writes to "Out" and Nexus program reads from "Out"

            Console.WriteLine("Nexus will use...");
            Console.WriteLine("  Named Pipe to Nexus Program: " + pipeNameOut);
            Console.WriteLine("  Named Pipe from Nexus Program: " + pipeNameIn);
            Console.WriteLine("  SocketIO Server URL: " + serverURL);


            // set up SocketIO
            //            var uri = new Uri("http://192.168.1.12:3000/");
            //            var uri = new Uri("https://Socketio-Simple-Chat.brightbird.repl.co");
            var uri = new Uri(serverURL);
            socket = new SocketIO(uri, new SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                Query = new Dictionary<string, string>
                {
                                        {"token", "V3" }
                },
            });

            socket.OnConnected += Socket_OnConnected;
            socket.OnPing += Socket_OnPing;
            socket.OnPong += Socket_OnPong;
            socket.OnDisconnected += Socket_OnDisconnected;
            socket.OnReconnectAttempt += Socket_OnReconnecting;

            // listener for chat messages
            socket.On("chat_message", response =>
            {
                Socket_OnChatMessage(response, qToPipe);
            });

            // listener for control messages
            socket.On("control_message", response =>
            {
                Socket_OnControlMessage(response, qToPipe);
            });

            // add code if other types of messages

            Launch();

            // end setup SocketIO

            // set up Named Pipes
            // see: https://learn.microsoft.com/en-us/windows/win32/ipc/transactions-on-named-pipes
            // and: https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.namedpipeserverstream?view=net-6.0
            //
            // NOTE: In and Out refer to use in the VBA program - VBA writes to "Out" and Nexus program reads from "Out"

            pipeStreamIn = new NamedPipeServerStream(pipeNameIn, PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            pipeStreamIn.WaitForConnectionAsync();
            pipeStreamOut = new NamedPipeServerStream(pipeNameOut, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            pipeStreamOut.WaitForConnectionAsync();
            // end setup Named Pipes

            // loop until...
            while (true)
            {
                // short pause, so this loop is not so resource greedy
                Thread.Sleep(5);

                // read pipe
                try
                {
                    receivedText = "";
                    if (pipeStreamOut.IsConnected)
                    {
                        //                        pipeStreamOut.ReadMode = PipeTransmissionMode.Message;
                        List<byte> intext = new List<byte>();
                        do
                        {
                            byte[] x = new byte[4096];
                            int read = 0;
                            read = pipeStreamOut.Read(x, 0, 4096);
                            Array.Resize(ref x, read);
                            intext.AddRange(x);
                        } while (!pipeStreamOut.IsMessageComplete);

                        receivedText = System.Text.Encoding.UTF8.GetString(intext.ToArray());
                        if (receivedText.Length > 0)
                        {
                            Console.WriteLine("Received Raw From Pipe: " + receivedText);
                        }

                        pipeStreamOut.Disconnect();
                        pipeStreamOut.WaitForConnectionAsync();
                    }
                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
                catch (IOException e)
                {
                    Console.WriteLine("ERROR: {0}", e.Message);
                }

                // check for termination command
                if (receivedText.Length > 0)
                {
                    // look for "|" separator characters, find tag string and content string
                    string[] parts = receivedText.Split('|');
                    if (parts != null)
                    {
                        bool foundTag = false;
                        string[] myItems = new string[2] { "", "" };
                        foreach (var part in parts)
                        {
                            if (foundTag)
                            {
                                myItems[1] = part;
                                break;
                            }

                            if (part == "chat_message")
                            {
                                foundTag = true;
                                myItems[0] = part;
                            }
                            if (part == "control_message")
                            {
                                foundTag = true;
                                myItems[0] = part;
                            }
                            if (part == "terminate")
                            {
                                foundTag = true;
                                myItems[0] = part;
                            }
                        }

                        // if we found two items, put them in the queue to the SocketIO server
                        if (myItems[0].Length > 0)
                        {
                            if (myItems[0] == "terminate")
                            {
                                TerminateNexus();
                            }
                            else
                            {
                                if (myItems[1].Length > 0)
                                {
                                    Console.WriteLine("Received From Pipe: " + myItems[0] + " = " + myItems[1]);
                                    EnqueToServer(qToServer, myItems[0], myItems[1]);
                                }
                            }
                        }

                    }
                }


                // get next item from qToPipe, send to Named Pipe
                if (pipeStreamIn != null)
                {
                    if (pipeStreamIn.IsConnected)
                    {
                        if (pipeStreamIn.CanWrite)
                        {
                            // check for the next item queued to go to the NamedPipe,
                            // with "|" character as separator and leading/trailing characters
                            receivedText = NextPipeQueueItem(qToPipe);  // should be either be useful text, or "|||"
                            if (receivedText != "|||")
                            {
                                Console.WriteLine("Sent To Pipe: " + receivedText);
                                pipeStreamIn.Write(System.Text.Encoding.UTF8.GetBytes(receivedText), 0, System.Text.Encoding.UTF8.GetBytes(receivedText).Length);
                            }


                            // this seems to make VBA happy, produces EOF when VBA opens pipe to read
                            pipeStreamIn.Close();
                            pipeStreamIn.Dispose();
                            pipeStreamIn = new NamedPipeServerStream(pipeNameIn, PipeDirection.Out, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                            pipeStreamIn.WaitForConnectionAsync();

                        }
                    }
                }

                // check other queue
                SendFromServerQueueToServer(qToServer);
            }

            async Task Launch()
            {
                await socket.ConnectAsync();
            }
        }

        private static void TerminateNexus()
        {
            Environment.Exit(0); // bail out
        }


        private static void Socket_OnChatMessage(SocketIOResponse resp, ConcurrentQueue<qStruct> whichQueue)
        {
            string info = resp.GetValue<string>();
            EnqueToPipe(whichQueue, "chat_message", info);
            Console.WriteLine("Chat message from SocketIO server: " + info);
        }

        private static void Socket_OnControlMessage(SocketIOResponse resp, ConcurrentQueue<qStruct> whichQueue)
        {
            string info = resp.GetValue<string>();
            EnqueToPipe(whichQueue, "chat_message", info);
            Console.WriteLine("Control message from SocketIO server: " + info);
        }


        private static void Socket_OnReconnecting(object sender, int e)
        {
            Console.WriteLine($"{DateTime.Now} Reconnecting: attempt = {e}");
        }

        private static void Socket_OnDisconnected(object sender, string e)
        {
            Console.WriteLine("disconnect: " + e);
        }

        private static async void Socket_OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Socket_OnConnected");
            var socket = sender as SocketIO;
            Console.WriteLine("Socket.Id:" + socket.Id);
        }

        private static void Socket_OnPing(object sender, EventArgs e)
        {
            Console.WriteLine("Ping");
        }

        private static void Socket_OnPong(object sender, TimeSpan e)
        {
            Console.WriteLine("Pong: " + e.TotalMilliseconds);
        }

        private static void EnqueToPipe(ConcurrentQueue<qStruct> whichQueue, string tag, string content)
        {
            var pipeItem = new qStruct();
            pipeItem.tag = tag;
            pipeItem.content = content;
            whichQueue.Enqueue(pipeItem);
        }
        private static void EnqueToServer(ConcurrentQueue<qStruct> whichQueue, string tag, string content)
        {
            var pipeItem = new qStruct();
            pipeItem.tag = tag;
            pipeItem.content = content;
            whichQueue.Enqueue(pipeItem);
        }

        // returns tag and content, from queue to NamedPipe,
        // with "|" character as separator and leading/trailing characters
        private static string NextPipeQueueItem(ConcurrentQueue<qStruct> whichQueue)
        {
            string info = "|||";
            var pipeItem = new qStruct();
            if (whichQueue.TryDequeue(out pipeItem))
            {
                info = "|" + pipeItem.tag + "|" + pipeItem.content + "|";
            }
            return info;
        }

        private static void SendFromServerQueueToServer(ConcurrentQueue<qStruct> whichQueue)
        {
            var serverItem = new qStruct();
            //            serverItem.tag = "x";
            //            serverItem.content = "z";
            if (whichQueue.TryDequeue(out serverItem))
            {
                if (serverItem != null)
                {
                    socket.EmitAsync(serverItem.tag, serverItem.content);
                }
            }
        }


    }
}