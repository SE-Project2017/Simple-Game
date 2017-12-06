using System;

using App;

using Barebones.Networking;

using MsfWrapper;

using Multiplayer;
using Multiplayer.Packets;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameResultDialog : MonoBehaviour
    {
        private Guid mMatchID;

        [SerializeField]
        private GameObject mDialogPanel = null;

        [SerializeField]
        private Text mLoadingText = null;

        [SerializeField]
        private GameObject mYourUpArrow = null;

        [SerializeField]
        private GameObject mYourDownArrow = null;

        [SerializeField]
        private GameObject mOpponentUpArrow = null;

        [SerializeField]
        private GameObject mOpponentDownArrow = null;

        [SerializeField]
        private Text mYourMmrText = null;

        [SerializeField]
        private Text mYourMmrChangeText = null;

        [SerializeField]
        private Text mYourNameText = null;

        [SerializeField]
        private Text mOpponentMmrText = null;

        [SerializeField]
        private Text mOpponentMmrChangeText = null;

        [SerializeField]
        private Text mOpponentNameText = null;

        [SerializeField]
        private Text mResultText = null;

        public void Start()
        {
            foreach (var child in mDialogPanel.GetComponentsInChildren<CanvasGroup>())
            {
                child.alpha = 0;
            }
            mLoadingText.gameObject.SetActive(true);

            MsfContext.Client.Connection.Peer.SendMessage(
                MessageHelper.Create(
                    (short) OperationCode.ClientQueryGameResult,
                    new ClientQueryGameResultPacket {MatchID = mMatchID}),
                (status, response) =>
                {
                    if (status != ResponseStatus.Success)
                    {
                        mLoadingText.text = "Error";
                        return;
                    }

                    mLoadingText.gameObject.SetActive(false);
                    foreach (var child in mDialogPanel.GetComponentsInChildren<CanvasGroup>())
                    {
                        child.alpha = 1;
                    }

                    var packet = response.Deserialize(new ClientGameResultPacket());

                    switch (packet.Result)
                    {
                        case ServerController.GameResult.NotStarted:
                            throw new NotImplementedException();
                        case ServerController.GameResult.Draw:
                            SetDraw();
                            break;
                        case ServerController.GameResult.PlayerAWon:
                            if (packet.PlayerType == ServerController.PlayerType.PlayerA)
                            {
                                SetVictory();
                            }
                            else
                            {
                                SetDefeat();
                            }
                            break;
                        case ServerController.GameResult.PlayerBWon:
                            if (packet.PlayerType == ServerController.PlayerType.PlayerB)
                            {
                                SetVictory();
                            }
                            else
                            {
                                SetDefeat();
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    int yourMmr;
                    int yourMmrChange;
                    int opponentMmr;
                    int opponentMmrChange;
                    switch (packet.PlayerType)
                    {
                        case ServerController.PlayerType.PlayerA:
                            mYourNameText.text = packet.PlayerAName;
                            mOpponentNameText.text = packet.PlayerBName;

                            yourMmr = packet.PlayerAMmr;
                            opponentMmr = packet.PlayerBMmr;

                            yourMmrChange = packet.PlayerAMmrChange;
                            opponentMmrChange = packet.PlayerBMmrChange;
                            break;
                        case ServerController.PlayerType.PlayerB:
                            mYourNameText.text = packet.PlayerBName;
                            mOpponentNameText.text = packet.PlayerAName;

                            yourMmr = packet.PlayerBMmr;
                            opponentMmr = packet.PlayerAMmr;

                            yourMmrChange = packet.PlayerBMmrChange;
                            opponentMmrChange = packet.PlayerAMmrChange;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    mYourMmrText.text = string.Format("Your MMR: {0}", yourMmr);
                    mYourMmrChangeText.text =
                        string.Format("MMR Change: {0:+#;-#;0}", yourMmrChange);
                    mOpponentMmrText.text = string.Format("Opponent MMR: {0}", opponentMmr);
                    mOpponentMmrChangeText.text =
                        string.Format("MMR Change: {0:+#;-#;0}", opponentMmrChange);
                });
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        public static GameResultDialog Show(Guid matchID)
        {
            var dialog = Instantiate(GlobalContext.Instance.GameResultDialogPrefab,
                FindObjectOfType<Canvas>().transform).GetComponent<GameResultDialog>();
            dialog.mMatchID = matchID;
            dialog.transform.SetSiblingIndex(ScreenTransition.Instance.transform.GetSiblingIndex());
            return dialog;
        }

        private void SetVictory()
        {
            mResultText.text = "Victory!";
            mYourDownArrow.SetActive(false);
            mYourUpArrow.SetActive(true);
            mOpponentUpArrow.SetActive(false);
            mOpponentDownArrow.SetActive(true);
        }

        private void SetDefeat()
        {
            mResultText.text = "Defeat!";
            mYourUpArrow.SetActive(false);
            mYourDownArrow.SetActive(true);
            mOpponentDownArrow.SetActive(false);
            mOpponentUpArrow.SetActive(true);
        }

        private void SetDraw()
        {
            mResultText.text = "Draw!";
            mYourUpArrow.SetActive(false);
            mYourDownArrow.SetActive(false);
            mOpponentUpArrow.SetActive(false);
            mOpponentDownArrow.SetActive(false);
        }
    }
}
