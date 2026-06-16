namespace ShopPOS.Domain.Security;

public static class SecurityQuestionCatalog
{
    public static IReadOnlyList<string> StandardQuestions { get; } =
    [
        "What is your primary school name?",
        "What is your birth city?",
        "What is your mother's maiden name?",
        "What was the name of your first pet?",
        "What is your favorite childhood teacher's name?",
        "What street did you grow up on?"
    ];
}
