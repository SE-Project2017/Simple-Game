namespace App
{
    public enum OperationCode : short
    {
        StartSearchGame,
        GameFound,
        GameServerSpawned,
        GameEnded,
        QuerySearchStatus,

        UploadSingleplayerResult,
    }
}
