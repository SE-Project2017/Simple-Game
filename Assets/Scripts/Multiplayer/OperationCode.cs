namespace Multiplayer
{
    public enum OperationCode : short
    {
        StartSearchGame,
        GameFound,
        GameServerSpawned,
        GameEnded,

        QuerySearchStatus,
    }
}
