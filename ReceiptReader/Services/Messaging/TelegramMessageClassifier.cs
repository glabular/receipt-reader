using Telegram.Bot.Types;

namespace ReceiptReader.Services.Messaging;

internal sealed class TelegramMessageClassifier
{
    public Enums.MessageType Classify(Message message)
    {
        if (message.MediaGroupId is not null)
        {
            return Enums.MessageType.Album;
        }

        if (message.Photo is not null && message.Photo.Length > 0)
        {
            return Enums.MessageType.Photo;
        }

        if (!string.IsNullOrWhiteSpace(message.Text) && message.Text.Trim().StartsWith('/'))
        {
            return Enums.MessageType.Command;
        }

        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            return Enums.MessageType.Text;
        }

        return Enums.MessageType.Other;
    }
}
