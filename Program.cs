using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace spawsh
{
    class Program
    {

        static string server = "gemini.circumlunar.space";
        static string page = "/";
        static string[] LineBuffer;
        static string[] linksInPage;
        static bool inInteractive = false;
        static int selectedLinkIndex = -1;
        static string currentPage;

        static void Main(string[] args)
        {

            string server = "gemini.circumlunar.space";
            string page = "/";
            bool validProtocol = true;

            int windowLineCount = Console.WindowHeight;            

            if (args.Length > 0)
            {
                string hostArgument = args[0];

                if (hostArgument.Contains("gemini://"))
                {
                    hostArgument = hostArgument.Trim();
                    hostArgument = hostArgument.Substring(9);
                }
                else if (hostArgument.Contains("https://") || hostArgument.Contains("http://") || hostArgument.Contains("gopher://"))
                {
                    Console.WriteLine("Protocol not supported.");
                    validProtocol = false;
                }

                if (hostArgument.Contains("/"))
                {
                    int firstSlashIndex = hostArgument.IndexOf('/');
                    server = hostArgument.Remove(firstSlashIndex);
                    page = hostArgument.Substring(firstSlashIndex, hostArgument.Length - firstSlashIndex);
                }
                else
                {
                    server = hostArgument;
                }
                if (validProtocol)
                {
                    TcpClient client = new TcpClient(server, 1965);


                    using (SslStream sslStream = new SslStream(client.GetStream(), false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                    {
                        sslStream.AuthenticateAsClient(server);
                        byte[] messageToSend = Encoding.UTF8.GetBytes("gemini://" + server + page + '\r' + '\n');
                        sslStream.Write(messageToSend);

                        string responseData = ReadMessage(sslStream);

                        handleResponse(responseData);

                    }
                    client.Close();
                }
            }
            else
            {
                inInteractive = true;
                LineBuffer = new string[windowLineCount];

                buildRequest(server + page);
                LineBuffer = fetchPage();
                linksInPage = buildLinkSet(LineBuffer);

                while (inInteractive)
                {
                    interactiveLoop();
                }
            }

        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static void interactiveLoop()
        {
            Console.Clear();

            for (int i = 0; i < Console.WindowWidth - LineBuffer[0].Length; i++)
            {
                Console.Write(" ");    
            }
            Console.WriteLine(LineBuffer[0]);

            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write("-");
            }
            Console.Write('\n');

            for (int i = 1; i < LineBuffer.Length; i++)
            {
                if (LineBuffer[i] != null)
                {
                    if (LineBuffer[i].Length < Console.WindowWidth)
                    {
                        Console.WriteLine(LineBuffer[i]);
                    }
                    else if (LineBuffer[i].Substring(0, 2) == "=>")
                    {
                        Console.WriteLine(LineBuffer[i]);
                    }
                    else
                    {
                        lineWrapString(LineBuffer[i]);
                    }
                }
            }

            if (selectedLinkIndex != -1)
            {
                Console.WriteLine(linksInPage[selectedLinkIndex]);
            }

            ConsoleKeyInfo keyRead = Console.ReadKey();

            if (keyRead.Key == ConsoleKey.Escape)
            {
                inInteractive = false;
            }
            else if (keyRead.Key == ConsoleKey.Enter)
            {
                string newInput;

                if (selectedLinkIndex == -1)
                {
                    Console.Write("url: ");
                    page = "";
                    newInput = Console.ReadLine();
                }
                else
                {
                    newInput = linksInPage[selectedLinkIndex];
                }

                if (newInput.Length != 0)
                {
                    buildRequest(newInput);

                    string[] fetchedPage = fetchPage();

                    LineBuffer = fetchedPage;

                    if (LineBuffer[0] == "No such host is known.")
                    {
                        linksInPage = new string[0];
                    }
                    else
                    {
                        linksInPage = buildLinkSet(LineBuffer);
                    }
                    

                    selectedLinkIndex = -1;
                }
                
            }
            else if (keyRead.Key == ConsoleKey.RightArrow)
            {
                if (selectedLinkIndex < linksInPage.Length - 1)
                {
                    selectedLinkIndex++;
                }
            }
            else if (keyRead.Key == ConsoleKey.LeftArrow)
            {
                if (selectedLinkIndex > -1)
                {
                    selectedLinkIndex--;
                }
            }

            if (selectedLinkIndex < 0)
            {
                selectedLinkIndex = -1;
            }
        }

        static string[] fetchPage()
        {
            TcpClient client = new TcpClient();
            string responseData = "";
            string[] fetchedOutput = new string[1024];

            try
            {
                client.Connect(server, 1965);
            }
            catch (SocketException error)
            {
                responseData = error.Message;
                fetchedOutput[0] = error.Message;
            }

            if (client != null && client.Connected)
            {
                using (SslStream sslStream = new SslStream(client.GetStream(), false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                {
                    sslStream.AuthenticateAsClient(server);

                    byte[] messageToSend = Encoding.UTF8.GetBytes("gemini://" + server + page + '\r' + '\n');
                    sslStream.Write(messageToSend);

                    responseData = ReadMessage(sslStream);

                    fetchedOutput = handleResponse(responseData);

                }
            }

            
            client.Close();

            return fetchedOutput;
        }

        static bool buildRequest(string inputString)
        {
            if (inputString.Contains("gemini://"))
            {
                inputString = inputString.Trim();
                inputString = inputString.Substring(9);
            }
            else if (inputString.Contains("https://") || inputString.Contains("http://") || inputString.Contains("gopher://"))
            {
                Console.WriteLine("Protocol not supported.");
                return false;
            }

            if (inputString.Contains("/"))
            {
                int firstSlashIndex = inputString.IndexOf('/');

                if (firstSlashIndex == 0 || firstSlashIndex == 1)
                {
                    //Should be same server
                }
                else
                {
                    server = inputString.Remove(firstSlashIndex);
                }
                
                page = inputString.Substring(firstSlashIndex, inputString.Length - firstSlashIndex);
            }
            else if (inputString.Split('.').Length == 2 && inputString.Split('.')[1] == "gmi")
            {
                inputString = "/" + inputString;
                page = inputString;
            }
            else
            {
                server = inputString;
            }

            if (page.Length >= 2 && page[0] == '/' && page[1] == '/')
            {
                page = page.Substring(1);
            }

            return true;
        }

        static string[] buildLinkSet(string[] lines)
        {
            string[] linkSet = Array.Empty<string>();
            int counter = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 2)
                {
                    if (lines[i].Substring(0, 2) == "=>")
                    {
                        string[] biggerArray = new string[counter + 1];
                        for (int e = 0; e < linkSet.Length; e++)
                        {
                            biggerArray[e] = linkSet[e];
                        }
                        biggerArray[counter] = lines[i].Split(' ')[1];

                        if (biggerArray[counter].Contains('\t'))
                        {
                            biggerArray[counter] = biggerArray[counter].Split('\t')[0];
                        }

                        if (!biggerArray[counter].Contains('.'))
                        {
                            //These should be local links
                            biggerArray[counter] = server + "/" + biggerArray[counter];
                        }

                        linkSet = biggerArray;
                        counter++;
                    }
                }
            }

            return linkSet;
        }

        static string ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the client.
            // The client signals the end of the message using the
            // "<EOF>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                // Read the client's test message.
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF or an empty message.
                if (messageData.ToString().IndexOf("<EOF>") != -1)
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }

        static string[] handleResponse(string responseData)
        {
            string[] responseLines = responseData.Split('\n');
            string responseBody = "";

            string responseHeader = responseLines[0];


            if (responseLines.Length > 1)
            {
                string[] responseBodyLines = new string[responseLines.Length-1];
                for (int i = 1; i < responseLines.Length; i++)
                {
                    responseBodyLines[i - 1] = responseLines[i];
                }
                responseBody = string.Join('\n', responseBodyLines);

            }

            string responseCode = responseHeader.Split(' ')[0];

            if (responseCode == "20")
            {
                Console.WriteLine(responseBody);
            }
            else if (responseCode[0] == '3')
            {
                string redirectAddress = responseHeader.Split(' ')[1];

                if (buildRequest(redirectAddress))
                {
                    Console.WriteLine("Redirect to {0}. Fetching.", redirectAddress);

                    responseLines = fetchPage();

                    linksInPage = buildLinkSet(responseLines);
                }

                selectedLinkIndex = -1;
            }
            else if (responseCode == "50")
            {
                Console.WriteLine("Permanent Failure");
            }
            else if (responseCode == "51")
            {
                Console.WriteLine("File not found.");
            }
            else if (responseCode == "10")
            {
                string searchPageURL = server + page;
                Console.Write("Search term: ");
                string searchParams = Console.ReadLine();

                page += "?" + searchParams;

                buildRequest(server + page);
                Console.WriteLine("Searching for {0}", searchParams);

                responseLines = fetchPage();
                linksInPage = buildLinkSet(responseLines);
                selectedLinkIndex = -1;
            }

            return responseLines;
        }

        static void lineWrapString(string input)
        {

            if (input.Length > Console.WindowWidth)
            {
                //break it out into most characters before width breaking at a space
                int lastNiceBreak = -1;
                string outputString = "";
                for (int i = 0; i < Console.WindowWidth; i++)
                {
                    if (input[i] == ' ')
                    {
                        lastNiceBreak = i;
                    }
                }
                
                //write it out
                if (lastNiceBreak != -1)
                {
                    outputString = input.Substring(0, lastNiceBreak);
                    Console.WriteLine(outputString);
                    string remainingString = input.Substring(lastNiceBreak + 1);
                    //call this on the remaining string
                    lineWrapString(remainingString);
                }
                else
                {
                    Console.WriteLine(input);
                }
                

                
            }
            else
            {
                Console.WriteLine(input);
            }
        }

    }
}
