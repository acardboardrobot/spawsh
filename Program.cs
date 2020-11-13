using System;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace spawsh
{
    class Program
    {

        static void Main(string[] args)
        {

            string server = "gemini.circumlunar.space";
            string page = "/";

            if (args.Length > 0)
            {
                string hostArgument = args[0];

                Console.WriteLine(hostArgument);

                if (hostArgument.Contains("/"))
                {
                    int firstSlashIndex = hostArgument.IndexOf('/');

                    server = hostArgument.Remove(firstSlashIndex);
                    page = hostArgument.Substring(firstSlashIndex, hostArgument.Length - firstSlashIndex);

                    Console.WriteLine(server);
                    Console.WriteLine(page);
                }
                else
                {
                    server = hostArgument;
                }
                
            }

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

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
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

        static void handleResponse(string responseData)
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
                Console.WriteLine("Redirect to {0}", responseHeader.Split(' ')[1]);
                Console.WriteLine("Try again at new url above.");
            }
            else if (responseCode == "50")
            {
                Console.WriteLine("Permanent Failure");
            }
            else if (responseCode == "51")
            {
                Console.WriteLine("File not found.");
            }
        }

    }
}
