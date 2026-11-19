namespace Multiplayer
{
    public enum GameOperationCode : short
    {
        Ping,
        JoinGame,
        RegisterComplete,
        FrameUpdate,
        PlayerEnded,
        GameResult,
    }
}
