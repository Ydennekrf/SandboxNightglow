

public interface IGameManager
{
    void Initialize();

    void GetAllItems();

    void GetAllScenes();

    void GetAllDialog();

    void LoadSavedGame(int id);
    void SaveCurrentGame();

    void StartNewGame();

    void SaveGameSettings();

    void QuitGame();

    void ReturnToMainMenu();
}