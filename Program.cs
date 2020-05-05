using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using VideoLibrary;
using System.Net.Mail;
using System.Text;
using System.Net;
using System.Net.Mime;

namespace Main
{
    internal class Program
    {
        /// <summary>
        /// Console app para baixar e enviar o programa Manhã de Luz para um email de destino.
        /// Para funcionar é necessário ter uma conta no serviço mailjet.com
        /// O programa pode ser configurado para rodar todo dia de manhã pelo gerenciador de tarefas: taskschd.msc
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            var exportPath = @"C:\ManhadeLuzDownloads\";

            Console.WriteLine("Playlist URL: ");
            var playlistUrl = "https://www.youtube.com/playlist?list=UUCEa9iCV6pX3DhT0jEa78ZQ";

            if (string.IsNullOrWhiteSpace(playlistUrl))
            {
                Console.WriteLine("Endereço Inválido. Saindo...");
                return;
            }

            if (!playlistUrl.ToLower().Contains("youtube.com"))
            {
                Console.WriteLine("Não é uma lista do Youtube. Saindo...");
                return;
            }

            if (!playlistUrl.ToLower().Contains("playlist?list="))
            {
                Console.WriteLine("Não é uma lista do Youtube. Saindo...");
                return;
            }

            var pathPlaylist = playlistUrl
                .Replace("https", "")
                .Replace("http", "")
                .Replace("://", "")
                .Replace("www.", "")
                .Replace("youtube.com/", "").Trim();

            var client = new HttpClient();
            client.BaseAddress = new Uri("https://www.youtube.com");
            var result = client.GetAsync(pathPlaylist).Result;
            var conteudo = result.Content.ReadAsStringAsync().Result;
            var links = ExtrairLinksPlaylist(conteudo);

            if (links.Count == 0)
            {
                Console.WriteLine("Nenhum video encontrado na playlist. Saindo do programa...");
                return;
            }

            var titulo = ExtrairTitulo(conteudo);
            var baseDir = $@"{exportPath}\{titulo}\";
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            var youtube = YouTube.Default;
            var engine = new Engine();

            var vid = youtube.GetVideo(links[0]);

            Console.WriteLine(vid.FullName);
            if (File.Exists(baseDir + vid.FullName))
            {
                Console.WriteLine(" - Arquivo já existe. Pulando...");
            }

            File.WriteAllBytes(baseDir + vid.FullName, vid.GetBytes());
            var inputFile = new MediaFile { Filename = baseDir + vid.FullName };
            var outputFile = new MediaFile { Filename = $"{baseDir + vid.FullName}.mp3" };

            string file = baseDir + vid.FullName + ".mp3";
            engine.GetMetadata(inputFile);
            engine.Convert(inputFile, outputFile);

            //Console.WriteLine(file);
            Console.WriteLine("Aguarde. Enviando o email com anexo ...");

            String APIKey = "API_KEY";
            String SecretKey = "SECRET";

            //crio objeto responsável pela mensagem de email
            MailMessage objEmail = new MailMessage();

            //rementente do email
            objEmail.From = new MailAddress("EMAILFROM");

            //email para resposta(quando o destinatário receber e clicar em responder, vai para:)
            objEmail.ReplyToList.Add("EMAILREPLY");

            //destinatário(s) do email(s). Obs. pode ser mais de um, pra isso basta repetir a linha
            //abaixo com outro endereço
            objEmail.To.Add("EMAILTO");

            //se quiser enviar uma cópia oculta pra alguém, utilize a linha abaixo:
            //objEmail.Bcc.Add("oculto@provedor.com.br");

            //prioridade do email
            objEmail.Priority = MailPriority.Normal;

            //utilize true pra ativar html no conteúdo do email, ou false, para somente texto
            objEmail.IsBodyHtml = true;

            //Assunto do email
            objEmail.Subject = "Programa Manhã de Luz - " + DateTime.Now.ToString("dd/MM/yyyy");

            //corpo do email a ser enviado
            objEmail.Body = "Último Programa manhã de Luz anexado ao Email";

            Attachment data = new Attachment(file, MediaTypeNames.Application.Octet);
            // Add time stamp information for the file.
            ContentDisposition disposition = data.ContentDisposition;
            disposition.CreationDate = System.IO.File.GetCreationTime(file);
            disposition.ModificationDate = System.IO.File.GetLastWriteTime(file);
            disposition.ReadDate = System.IO.File.GetLastAccessTime(file);

            objEmail.Attachments.Add(data);

            //codificação do assunto do email para que os caracteres acentuados serem reconhecidos.
            objEmail.SubjectEncoding = Encoding.GetEncoding("ISO-8859-1");

            //codificação do corpo do emailpara que os caracteres acentuados serem reconhecidos.
            objEmail.BodyEncoding = Encoding.GetEncoding("ISO-8859-1");

            //cria o objeto responsável pelo envio do email
            SmtpClient objSmtp = new SmtpClient("in.mailjet.com", 587);


            //endereço do servidor SMTP(para mais detalhes leia abaixo do código)
            //objSmtp.Host = "in.mailjet.com";

            objSmtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            objSmtp.EnableSsl = true;
            //objSmtp.Port = 587;
            objSmtp.UseDefaultCredentials = false;

            //para envio de email autenticado, coloque login e senha de seu servidor de email
            //para detalhes leia abaixo do código
            objSmtp.Credentials = new NetworkCredential(APIKey, SecretKey);

            //envia o email
            objSmtp.Send(objEmail);

            /* rotina para enviar toda a lista. 
             * i = 0;
            foreach (var linkVideo in links)
            {
                var vid = youtube.GetVideo(linkVideo);

                Console.WriteLine($"{i}) " + vid.FullName);
                if (File.Exists(baseDir + vid.FullName))
                {
                    Console.WriteLine(" - Arquivo já existe. Pulando...");
                    continue;
                }

                File.WriteAllBytes(baseDir + vid.FullName, vid.GetBytes());
                var inputFile = new MediaFile { Filename = baseDir + vid.FullName };
                var outputFile = new MediaFile { Filename = $"{baseDir + vid.FullName}.mp3" };
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
                i++;
            }
            */
            engine.Dispose();
        }

        private static string ExtrairTitulo(string conteudo)
        {
            try
            {
                var t = "<title>";
                var ini = conteudo.IndexOf(t) + t.Length;
                var fim = conteudo.IndexOf("</title>");
                var titulo = conteudo.Substring(ini, fim - ini);
                titulo = titulo.EndsWith(" - YouTube") ? titulo.Remove(titulo.LastIndexOf(" - YouTube")) : titulo;
                return titulo.Trim();
            }
            catch (Exception)
            {
                return "Youtube";
            }
        }

        private static List<string> ExtrairLinksPlaylist(string html)
        {
            var linksPlaylist = new List<string>();

            var w = "/watch?v=";
            var padraoLink = $"<a class=\"pl-video-title-link yt-uix-tile-link yt-uix-sessionlink  spf-link \" dir=\"ltr\" href=\"{w}";
            var idx = html.IndexOf(padraoLink, 0);

            while (idx > -1)
            {
                var idxInicioLink = idx + padraoLink.Length - w.Length;
                var idxFimLink = html.IndexOf("&", idxInicioLink);
                var pedacoLink = html.Substring(idxInicioLink, idxFimLink - idxInicioLink);
                linksPlaylist.Add(pedacoLink);

                idx = html.IndexOf(padraoLink, idxFimLink);
            }

            return linksPlaylist;
        }
    }
}