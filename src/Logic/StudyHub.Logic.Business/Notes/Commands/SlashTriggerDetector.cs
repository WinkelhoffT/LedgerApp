namespace StudyHub.Logic.Business.Notes.Commands;

public sealed class SlashTriggerDetector : ISlashTriggerDetector
{
    public SlashTrigger? Detect(string text, int caretIndex)
    {
        if (caretIndex <= 0 || caretIndex > text.Length)
        {
            return null;
        }

        var slashIndex = -1;

        for (var i = caretIndex - 1; i >= 0; i--)
        {
            var character = text[i];

            if (character == '/')
            {
                slashIndex = i;
                break;
            }

            if (char.IsWhiteSpace(character))
            {
                return null;
            }
        }

        if (slashIndex < 0)
        {
            return null;
        }

        var precededByWhitespace = slashIndex == 0 || char.IsWhiteSpace(text[slashIndex - 1]);
        if (!precededByWhitespace)
        {
            return null;
        }

        var query = text[(slashIndex + 1)..caretIndex];
        return new SlashTrigger(slashIndex, query);
    }
}
