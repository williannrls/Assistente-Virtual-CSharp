﻿using ArgosAutomation.Abstractions;
using ArgosAutomation.Databases;
using ArgosAutomation.Jobs;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ArgosAutomation
{
    /// <summary>
    /// Classe maniopuladora de atualizações recebidas pelo bot.
    /// </summary>
    internal class UpdateHandler
    {
        /// <summary>
        /// Identificador único da atualização
        /// </summary>
        static public int UpdateId { get; set; }
        /// <summary>
        /// Identificador único da mensagem dentro do chat.
        /// </summary>
        static public int? MessageId { get; set; }
        /// <summary>
        /// Mensagem de texto.
        /// </summary>
        static public string? MessageText { get; set; }
        /// <summary>
        /// Data e hora na qual a mensagem foi enviada.
        /// </summary>
        static public DateTime MessageDate { get; set; }
        /// <summary>
        /// Primeiro nome da pessoa
        /// </summary>
        static public string? FirstName { get; set; }
        /// <summary>
        /// Segundo nome da pessoa.
        /// </summary>
        static public string? LastName { get; set; }
        /// <summary>
        /// Identificador único do chat.
        /// </summary>
        static public long ChatId { get; set; }
        /// <summary>
        /// Nome do chat (Para atualizações recebidas em grupos e canais).
        /// </summary>
        static public string? ChatTitle { get; set; }
        /// <summary>
        /// Identificador único do usuário.
        /// </summary>
        static public long UserId { get; set; }
        /// <summary>
        /// Nome único do usuário.
        /// </summary>
        static public string? UserName { get; set; }
        /// <summary>
        /// Armazena se o painel está sendo gerado ou não.
        /// </summary>
        static public int BeingGenerated { get; set; }
        /// <summary>
        /// Objeto to tipo Report para fluxo de geração e envio de reports.
        /// </summary>
        public static Report? Report { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public static KeyboardButton btn;

        /// <summary>
        /// Método responsável por direcionar o bot as respostas de acordo com o tipo de atualização e conteúdo.
        /// </summary>
        /// <param name="botClient">Instância do bot na API do Telegram.</param>
        /// <param name="Update">Objeto que representa a atualização em questão, nela contém as informações necessárias para manipular as atualizações.</param>
        /// <returns></returns>
        async public static Task Init(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Atribuindo valores as propriedades.
            Console.WriteLine(@$" [{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] UpdateHandler: Nova atualização enviada ao bot.");
            UpdateId = update.Id;
            MessageId = update.Message.MessageId;
            MessageDate = update.Message.Date.ToLocalTime();
            FirstName = update.Message.From.FirstName;
            LastName = update.Message.From.LastName;
            ChatId = update.Message.Chat.Id;
            ChatTitle = update.Message.Chat.Title;
            UserId = update.Message.From.Id;
            UserName = update.Message.From.Username;
            Console.WriteLine(@$"    ID da atualização: {UpdateId}
    ID da mensagem: {MessageId}
    Data e hora: {MessageDate}
    Nome: {FirstName} {LastName}
    Chat ID: {ChatId}
    Título do chat: {ChatTitle}
    ID do usuário: {UserId}
    Usuário: {UserName}");

            // Obtém se o usuário da última atualização está permitido a enviar mensagens e receber respostas do bot.
            Odbc.Connect("ArgosAutomation", "DSN=SRVAZ31-ARGOS");
            string qry = "qryValidationChatId.txt";
            Odbc.dtm.CleanParamters(qry);
            Odbc.dtm.ParamByName(qry, ":CHATID", ChatId.ToString());
            DataTable dt = Odbc.dtm.ExecuteQuery(qry);
            //Odbc.dtm.Disconect();

            // Verificação de usuário.
            if (ChatId == long.Parse((string)dt.Rows[0]["ID"]))
            {
                // Verifica o tipo de atualização enviada.
                // Mensagem de texto.
                Console.WriteLine(@$"    Chat ID correspondente permitido: {long.Parse((string)dt.Rows[0]["ID"])}.");
                if (update.Message.Type == MessageType.Text)
                {
                    // Atribui o valor do texto enviado ao bot dentro da propriedade "MessageText" e faz um tratamento de caracteres para uma maior facilidade de acionar os gatilhos.
                    MessageText = update.Message.Text;
                    Console.WriteLine(@$"    Texto: {MessageText}");
                    string message = Tools.RemoveSpecialCharacters(Tools.RemoveDiacritics(MessageText).Replace(" ", "").ToLower(), true);

                    // Obtém da tabela a lista de comandos de painel e cria uma lista com todos esses comandos.
                    Odbc.Connect("ArgosAutomation", "DSN=SRVAZ31-ARGOS");
                    qry = "qryGetReports.txt";
                    dt = Odbc.dtm.ExecuteQuery(qry);
                    //Odbc.dtm.Disconect();
                    List<string?> reportsCommands = new();
                    List<string?> btnCD = new();
                    List<string?> btnAlfan = new();
                    List<string?> btnTransp = new();
                    List<string?> btnAcomOp = new();
                    for (int i = 0; i <= dt.Rows.Count - 1; i++)
                    {
                        reportsCommands.Add(dt.Rows[i]["COMANDO"].ToString());

                        if (dt.Rows[i]["OPERACAO"].ToString().Equals("CD"))
                        {
                            btnCD.Add(dt.Rows[i]["NOME"].ToString());
                        }
                        else if (dt.Rows[i]["OPERACAO"].ToString().Equals("Alfandegado"))
                        {
                            btnAlfan.Add(dt.Rows[i]["NOME"].ToString());
                        }
                        else if (dt.Rows[i]["OPERACAO"].ToString().Equals("Transporte"))
                        {
                            btnTransp.Add(dt.Rows[i]["NOME"].ToString());
                        }
                        else if (dt.Rows[i]["OPERACAO"].ToString().Equals("Acompanhamento Operacional"))
                        {
                            btnAcomOp.Add(dt.Rows[i]["NOME"].ToString());
                        }
                    }

                    //
                    Odbc.Connect("ArgosAutomation", "DSN=SRVAZ31-ARGOS");
                    qry = "qryGetGroupsTelegram.txt";
                    var dtb = Odbc.dtm.ExecuteQuery(qry);
                    List<long?> listGroups = new();

                    //
                    for (int i = 0; i <= dtb.Rows.Count - 1; i++)
                    {
                        listGroups.Add(long.Parse((string)dtb.Rows[i]["CHAT_ID"]));
                    }

                    // Verifica se a solicitação em questão foi de painel, caso o valor da variável "message" contenha na lista "reportsCommands" o código segue.
                    if (reportsCommands.Contains(message))
                    {
                        // Constrói o report solicitado pelo usuário através do comando.
                        Console.WriteLine(@$" [{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] UpdateHandler: Solicitação do painel de {MessageText}.");
                        Report = new(message);

                        if (Report.ChatIdGroup.Contains(ChatId) || ChatId == 5495003005 || ChatId == -975484125 || ChatId == -1001530604829)
                        {
                            // Verifica se o painel está ativado ou não.
                            if (Report.Enable == 1)
                            {
                                // Obtém os todos os valores da coluna "GERANDO" dentro da tabela t_painel_automation, verifica se há algum valor igual a true e grava o resultado dentro da propriedade BeingGenerated.
                                Console.WriteLine(@$" [{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] UpdateHandler: O painel de {MessageText} está ativo.");
                                BeingGenerated = Tools.HasTrueValueInColumn(dt, "GERANDO");

                                // Verifica se há algum painel sendo gerado.
                                if (BeingGenerated == 0)
                                {
                                    // Caso não tiver, começa a gerar o painel solicitado e faz o envio.

                                    await botClient.SendChatActionAsync(
                                        chatId: ChatId,
                                        chatAction: ChatAction.Typing,
                                        cancellationToken: cancellationToken);
                                    await botClient.SendTextMessageAsync(
                                        chatId: ChatId,
                                        text: $"🤖: Ok {FirstName}! Gerando dados de *{MessageText}*, envio em alguns instantes.",
                                        replyToMessageId: MessageId,
                                        replyMarkup: new ReplyKeyboardRemove(),
                                        disableNotification: true,
                                        parseMode: ParseMode.Markdown,
                                        cancellationToken: cancellationToken);
                                    await Report.Generate();
                                    await Report.ToSend(ChatId);

                                }
                                else
                                {
                                    // Caso algum painel esteja sendo gerado ele envia um alerta ao usuário.
                                    Console.BackgroundColor = ConsoleColor.Red;
                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                    Console.WriteLine(@$" [{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] UpdateHandler: Conflito entre de reports.");
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    await botClient.SendChatActionAsync(
                                        chatId: ChatId,
                                        chatAction: ChatAction.Typing,
                                        cancellationToken: cancellationToken);
                                    await botClient.SendTextMessageAsync(
                                        chatId: ChatId,
                                        text: $"🤖: {FirstName}, aguarde um momento eu estou executando a *{ReportJob.JobName}* no grupo de *{ReportJob.GroupName[0]}*. Aguarde uns instantes e solicite o painel de *{MessageText}* novamente.",
                                        replyToMessageId: MessageId,
                                        replyMarkup: new ReplyKeyboardRemove(),
                                        disableNotification: true,
                                        parseMode: ParseMode.Markdown,
                                        cancellationToken: cancellationToken);
                                }
                            }
                            else
                            {
                                // Caso o painel em questão esteja desativado o bot envia um alerta ao usuário.
                                Console.BackgroundColor = ConsoleColor.DarkYellow;
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine(@$" [{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] UpdateHandler: O painel de {MessageText} não está ativo, abortando report.");
                                Console.BackgroundColor = ConsoleColor.Black;
                                Console.ForegroundColor = ConsoleColor.Gray;
                                await botClient.SendChatActionAsync(
                                    chatId: ChatId,
                                    chatAction: ChatAction.Typing,
                                    cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: {FirstName}, o painel de *{MessageText}* foi desativado automaticamente pois ele está em manutenção, o time de dados da TI/Torre de Controle já está atuando e para mais informações entre em contato com a Torre de Controle!",
                                    replyToMessageId: MessageId,
                                    replyMarkup: new ReplyKeyboardRemove(),
                                    disableNotification: true,
                                    parseMode: ParseMode.Markdown,
                                    cancellationToken: cancellationToken);
                            }

                        }
                        else
                        {
                            await botClient.SendChatActionAsync(
                                chatId: ChatId,
                                chatAction: ChatAction.Typing,
                                cancellationToken: cancellationToken);
                            await botClient.SendTextMessageAsync(
                                chatId: ChatId,
                                text: $"🤖: {FirstName}, só é permitida a solicitação do painel de *{MessageText}* no grupo *\"{Report.GroupName[0]}\"* aqui no telegram, talvez você não faça parte desse grupo mas caso faça, solicite-o lá.",
                                replyToMessageId: MessageId,
                                replyMarkup: new ReplyKeyboardRemove(),
                                disableNotification: true,
                                parseMode: ParseMode.Markdown,
                                cancellationToken: cancellationToken);
                        }

                    }

                    // Comando genérico para os grupos operacionais.
                    if (message.Equals("argosmeenvieumpainel"))
                    {
                        //
                        var replyButton = new ReplyKeyboardMarkup(btn);
                        var r = new List<KeyboardButton[]>();
                        var c = new List<KeyboardButton>();

                        //
                        switch (ChatId)
                        {
                            #region Produção

                            case -524861458: // CD

                                for (int i = 0; i <= btnCD.Count - 1; i++)
                                {
                                    c.Add(new KeyboardButton("" + btnCD[i]));
                                    r.Add(c.ToArray());
                                    c = new List<KeyboardButton>();
                                }
                                if (c.Count >= 0) { r.Add(c.ToArray()); }
                                replyButton.Keyboard = r.ToArray();

                                await botClient.SendChatActionAsync(
                                    chatId: ChatId,
                                    chatAction: ChatAction.Typing,
                                    cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Claro! {FirstName}. Qual você quer ver?",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    replyMarkup: replyButton,
                                    cancellationToken: cancellationToken);
                                break;

                            case -617355105: // Transporte
                                for (int i = 0; i <= btnTransp.Count - 1; i++)
                                {
                                    c.Add(new KeyboardButton("" + btnTransp[i]));
                                    r.Add(c.ToArray());
                                    c = new List<KeyboardButton>();
                                }
                                if (c.Count >= 0) { r.Add(c.ToArray()); }
                                replyButton.Keyboard = r.ToArray();

                                await botClient.SendChatActionAsync(
                                    chatId: ChatId,
                                    chatAction: ChatAction.Typing,
                                    cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Claro! {FirstName}. Qual você quer ver?",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    replyMarkup: replyButton,
                                    cancellationToken: cancellationToken);
                                break;

                            case -1001540853126: // Alfandegado
                                for (int i = 0; i <= btnAlfan.Count - 1; i++)
                                {
                                    c.Add(new KeyboardButton("" + btnAlfan[i]));
                                    r.Add(c.ToArray());
                                    c = new List<KeyboardButton>();
                                }
                                if (c.Count >= 0) { r.Add(c.ToArray()); }
                                replyButton.Keyboard = r.ToArray();

                                await botClient.SendChatActionAsync(
                                    chatId: ChatId,
                                    chatAction: ChatAction.Typing,
                                    cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Claro! {FirstName}. Qual você quer ver?",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    replyMarkup: replyButton,
                                    cancellationToken: cancellationToken);
                                break;

                            case -1001773173154: // Acompanhamento Operacional
                                for (int i = 0; i <= btnAcomOp.Count - 1; i++)
                                {
                                    c.Add(new KeyboardButton("" + btnAcomOp[i]));
                                    r.Add(c.ToArray());
                                    c = new List<KeyboardButton>();
                                }
                                if (c.Count >= 0) { r.Add(c.ToArray()); }
                                replyButton.Keyboard = r.ToArray();

                                await botClient.SendChatActionAsync(
                                     chatId: ChatId,
                                     chatAction: ChatAction.Typing,
                                     cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Claro! {FirstName}. Qual você quer ver?",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    replyMarkup: replyButton,
                                    cancellationToken: cancellationToken);
                                break;

                            #endregion

                            #region Teste

                            case -4050698543: // CD

                                for (int i = 0; i <= btnCD.Count - 1; i++)
                                {
                                    c.Add(new KeyboardButton("" + btnCD[i]));
                                    r.Add(c.ToArray());
                                    c = new List<KeyboardButton>();
                                }
                                if (c.Count >= 0) { r.Add(c.ToArray()); }
                                replyButton.Keyboard = r.ToArray();

                                await botClient.SendChatActionAsync(
                                    chatId: ChatId,
                                    chatAction: ChatAction.Typing,
                                    cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Claro! {FirstName}. Qual você quer ver?",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    replyMarkup: replyButton,
                                    cancellationToken: cancellationToken);
                                break;

                            case -4080450305: // Transporte
                                for (int i = 0; i <= btnTransp.Count - 1; i++)
                                {
                                    c.Add(new KeyboardButton("" + btnTransp[i]));
                                    r.Add(c.ToArray());
                                    c = new List<KeyboardButton>();
                                }
                                if (c.Count >= 0) { r.Add(c.ToArray()); }
                                replyButton.Keyboard = r.ToArray();

                                await botClient.SendChatActionAsync(
                                    chatId: ChatId,
                                    chatAction: ChatAction.Typing,
                                    cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Claro! {FirstName}. Qual você quer ver?",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    replyMarkup: replyButton,
                                    cancellationToken: cancellationToken);
                                break;

                            case -4077283492: // Alfandegado
                                for (int i = 0; i <= btnAlfan.Count - 1; i++)
                                {
                                    c.Add(new KeyboardButton("" + btnAlfan[i]));
                                    r.Add(c.ToArray());
                                    c = new List<KeyboardButton>();
                                }
                                if (c.Count >= 0) { r.Add(c.ToArray()); }
                                replyButton.Keyboard = r.ToArray();

                                await botClient.SendChatActionAsync(
                                    chatId: ChatId,
                                    chatAction: ChatAction.Typing,
                                    cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Claro! {FirstName}. Qual você quer ver?",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    replyMarkup: replyButton,
                                    cancellationToken: cancellationToken);
                                break;

                            case -4060828765: // Acompanhamento Operacional
                                for (int i = 0; i <= btnAcomOp.Count - 1; i++)
                                {
                                    c.Add(new KeyboardButton("" + btnAcomOp[i]));
                                    r.Add(c.ToArray());
                                    c = new List<KeyboardButton>();
                                }
                                if (c.Count >= 0) { r.Add(c.ToArray()); }
                                replyButton.Keyboard = r.ToArray();

                                await botClient.SendChatActionAsync(
                                     chatId: ChatId,
                                     chatAction: ChatAction.Typing,
                                     cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Claro! {FirstName}. Qual você quer ver?",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    replyMarkup: replyButton,
                                    cancellationToken: cancellationToken);
                                break;

                            #endregion

                            default: // Padrão
                                await botClient.SendChatActionAsync(
                                    chatId: ChatId,
                                    chatAction: ChatAction.Typing,
                                    cancellationToken: cancellationToken);
                                await botClient.SendTextMessageAsync(
                                    chatId: ChatId,
                                    text: $"🤖: Esse comando não é permitido nesse chat!",
                                    replyToMessageId: MessageId,
                                    disableNotification: true,
                                    cancellationToken: cancellationToken);
                                break;
                        }
                    }

                    // Comando para a atualização geral de todos os BI's de um grupo
                    if (message.Equals("argosnosatualize"))
                    {
                        //
                        Odbc.Connect("ArgosAutomation", "DSN=SRVAZ31-ARGOS");
                        qry = "qryGetReportsByGroup.txt";
                        Odbc.dtm.CleanParamters(qry);
                        Odbc.dtm.ParamByName(qry, ":CHAT_ID", ChatId.ToString());
                        var dtw = Odbc.dtm.ExecuteQuery(qry);

                        //
                        await botClient.SendChatActionAsync(
                            chatId: ChatId,
                            chatAction: ChatAction.Typing,
                            cancellationToken: cancellationToken);
                        await botClient.SendTextMessageAsync(
                            chatId: ChatId,
                            text: $"🤖: Claro, {FirstName}! Em alguns segundos começo os envios dos reports.",
                            replyToMessageId: MessageId,
                            replyMarkup: new ReplyKeyboardRemove(),
                            disableNotification: true,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: cancellationToken);

                        //
                        if (listGroups.Contains(ChatId))
                        {
                            for (int i = 0; i < dtw.Rows.Count; i++)
                            {
                                Report = new((string)dtw.Rows[i]["COMANDO_PAINEL"]);
                                await Report.Generate();
                                await Report.ToSend(ChatId);
                            }
                        }
                    }

                    // Grava todas as mensagens que o Argos recebe no banco de dados.
                    Odbc.Connect("ArgosAutomation", "DSN=SRVAZ31-ARGOS");
                    qry = "qryInsertUpdates.txt";
                    Odbc.dtm.CleanParamters(qry);
                    Odbc.dtm.ParamByName(qry, ":ID_TABELA", Guid.NewGuid().ToString());
                    Odbc.dtm.ParamByName(qry, ":ATUALIZACAO_ID", UpdateId.ToString());
                    Odbc.dtm.ParamByName(qry, ":ID_USUARIO", UserId.ToString());
                    Odbc.dtm.ParamByName(qry, ":CHAT_ID", ChatId.ToString());
                    Odbc.dtm.ParamByName(qry, ":TITULO_CHAT", ChatTitle);
                    Odbc.dtm.ParamByName(qry, ":TEXTO", MessageText);
                    Odbc.dtm.ParamByName(qry, ":NOME", $"{FirstName} {LastName}");
                    Odbc.dtm.ParamByName(qry, ":USUARIO", UserName);
                    Odbc.dtm.ExecuteNonQuery(qry);
                    Console.WriteLine(@$" [{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] UpdateHandler: Insert na tabela argos.t_atualizacao_recebida_telegram da atualizaçao nº {MessageId} feito as {DateTime.Now:HH:mm:ss} foi realizado com exito.");
                    Console.WriteLine(" ");

                }
                else
                {
                    // 
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(@$" [{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] UpdateHandler: Atualização do tipo {update.Message.Type} não foi tratada.");
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }

            }
            else
            {
                // Mensagem de bloqueio para usuário não autorizado a falar como bot.
                Console.BackgroundColor = ConsoleColor.DarkYellow;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(@$" [{DateTime.Now:dd/MM/yyyy - HH:mm:ss}] UpdateHandler: Individuo sem autorização.");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Gray;
                await botClient.SendChatActionAsync(
                    chatId: ChatId,
                    chatAction: ChatAction.Typing,
                    cancellationToken: cancellationToken);
                await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: "Opa...Parece que não você *não* tem permissões necessárias para falar comigo 🔒. Solicite acesso aos meus administradores na Torre de Controle.",
                    replyToMessageId: MessageId,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);

                //
                await botClient.SendChatActionAsync(
                    chatId: ChatId,
                    chatAction: ChatAction.Typing,
                    cancellationToken: cancellationToken);
                await botClient.SendTextMessageAsync(
                    chatId: 5495003005,
                    text: @$"Individuo sem autorização 🔒 - {DateTime.Now}

Uma pessoa não autorizada está tentando falar comigo.

*Nome:* {FirstName} {LastName}
*Chat ID:* {ChatId}
*Data e hora:*: {MessageDate}",
                    replyToMessageId: MessageId,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);

            }
        }
    }
}
