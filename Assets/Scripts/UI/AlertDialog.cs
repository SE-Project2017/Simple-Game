using System;

using App;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class AlertDialog : MonoBehaviour
    {
        public GameObject Message;
        public GameObject Buttons;
        public Text MessageText;

        public void Close()
        {
            Destroy(gameObject);
        }

        public class Builder
        {
            private string mMessage;
            private string mNeutralButtonText;
            private Action mNeutralButtonCallback;

            public Builder SetMessage(string message)
            {
                mMessage = message;
                return this;
            }

            public Builder SetNeutralButton(string text, Action callback)
            {
                mNeutralButtonText = text;
                mNeutralButtonCallback = callback;
                return this;
            }

            public AlertDialog Show()
            {
                var dialog = Instantiate(GlobalContext.Instance.AlertDialogPrefab,
                    FindObjectOfType<Canvas>().transform).GetComponent<AlertDialog>();
                if (mMessage != null)
                {
                    dialog.Message.SetActive(true);
                    dialog.MessageText.text = mMessage;
                }
                if (mNeutralButtonText != null)
                {
                    dialog.Buttons.SetActive(true);
                    var button = Instantiate(GlobalContext.Instance.AlertDialogButtonPrefab,
                        dialog.Buttons.transform).GetComponent<Button>();
                    button.GetComponentInChildren<Text>().text = mNeutralButtonText;
                    button.onClick.AddListener(() =>
                    {
                        mNeutralButtonCallback();
                        dialog.Close();
                    });
                }
                return dialog;
            }
        }
    }
}
