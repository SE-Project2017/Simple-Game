namespace App
{
    public enum OperationCode : short
    {
        StartSearchGame,
        QuerySearchStatus,
        CancelSearch,
        GameFound,
        GameServerSpawned,
        GameEnded,

        UploadSingleplayerResult,
    }
}
