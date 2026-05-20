public static class Constants
{
    // Coin rewards by game mode
    public const int PUZZLE_SHOW_BASE_REWARD = 50;
    public const int CLASSIC_MODE_BASE_REWARD = 20;
    public const int TIME_ATTACK_BASE_REWARD = 30;
    public const int TIME_ATTACK_BONUS_PER_SECOND = 1;

    // Power-up costs
    public const int HINT_COST = 0;
    public const int REVEAL_COST = 0;
    public const int UNDO_COST = 0;

    // Starting values
    public const int STARTING_COINS = 0;
    public const int STARTING_HINTS = 0;
    public const int STARTING_REVEALS = 0;
    public const int STARTING_UNDOS = 0;

    // Game mechanics
    public const int MAX_WORD_LENGTH = 7;
    public const int MIN_WORD_LENGTH = 3;
    public const int STARTING_LIVES = 3;
    public const int MAX_LETTER_INPUT = 10;

    // Time Attack
    public const float TIME_ATTACK_START = 90f;
    public const float TIME_ATTACK_MIN = 30f;
    public const float TIME_ATTACK_DECREMENT = 5f;

    // Puzzle Show
    public const int MAX_TIERS = 10;
    public const int PUZZLES_PER_TIER = 5;
}
