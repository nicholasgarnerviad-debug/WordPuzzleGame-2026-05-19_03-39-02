public interface IGameMode
{
    void Initialize();
    void StartGame();
    void OnPuzzleComplete(int score);
    void OnGameOver();
    int GetCoinsEarned();
    string GetModeName();
}
