using UnityEngine;

using Utils;

namespace App
{
    public class AudioManager : Singleton<AudioManager>
    {
#pragma warning disable 649
        [SerializeField]
        private AudioSource mLockSound;

        [SerializeField]
        private AudioSource mLandSound;

        [SerializeField]
        private AudioSource mTetrominoSound;

        [SerializeField]
        private AudioSource mPreHoldSound;

        [SerializeField]
        private AudioSource mPreRotateSound;

        [SerializeField]
        private AudioSource mBackgroundMusic;

        [SerializeField]
        private AudioClip[] mTetrominoClips;

        [SerializeField]
        private AudioClip mSingleplayerLevel1Music;

        [SerializeField]
        private AudioClip mSingleplayerLevel2Music;

        [SerializeField]
        private AudioClip mMultiplayerMusic;
#pragma warning restore 649

        public void PlayLockSound()
        {
            mLockSound.Play();
        }

        public void PlayLandSound()
        {
            mLandSound.Play();
        }

        public void PlayTetrominoSound(Tetromino tetromino)
        {
            mTetrominoSound.clip = mTetrominoClips[(int) tetromino];
            mTetrominoSound.Play();
        }

        public void PlayPreHoldSound()
        {
            mPreHoldSound.Play();
        }

        public void PlayPreRotateSound()
        {
            mPreRotateSound.Play();
        }

        public void StopBackgroundMusic()
        {
            mBackgroundMusic.Stop();
        }

        public void PlaySingleplayerLevel1Music()
        {
            mBackgroundMusic.clip = mSingleplayerLevel1Music;
            mBackgroundMusic.Play();
        }

        public void PlaySingleplayerLevel2Music()
        {
            mBackgroundMusic.clip = mSingleplayerLevel2Music;
            mBackgroundMusic.Play();
        }

        public void PlayMultiplayerMusic()
        {
            mBackgroundMusic.clip = mMultiplayerMusic;
            mBackgroundMusic.Play();
        }
    }
}
