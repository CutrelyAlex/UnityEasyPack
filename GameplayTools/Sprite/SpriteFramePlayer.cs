using System.Collections;
using UnityEngine;

namespace EasyPack
{
    /// <summary>
    /// SpriteFramePlayer 用于在 Unity 中播放精灵帧动画。
    /// 支持播放、暂停、停止、恢复、切换帧等操作。
    /// </summary>
    public class SpriteFramePlayer : MonoBehaviour
    {
        /// <summary>
        /// 精灵帧数组。
        /// </summary>
        public Sprite[] frames;

        /// <summary>
        /// 播放速度（每帧间隔时间，单位：秒）。
        /// </summary>
        public float frameRate = 0.1f;

        /// <summary>
        /// SpriteRenderer 组件引用。
        /// </summary>
        private SpriteRenderer spriteRenderer;

        /// <summary>
        /// 当前帧索引。
        /// </summary>
        private int currentFrame = 0;

        /// <summary>
        /// 是否正在播放。
        /// </summary>
        private bool isPlaying = false;

        /// <summary>
        /// 播放协程的引用。
        /// </summary>
        private Coroutine playCoroutine;

        /// <summary>
        /// 初始化组件，获取或添加 SpriteRenderer。
        /// </summary>
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// 开始播放精灵帧动画。
        /// </summary>
        public void Play()
        {
            if (isPlaying) return;

            isPlaying = true;
            playCoroutine = StartCoroutine(PlayFrames());
        }

        /// <summary>
        /// 停止播放精灵帧动画。
        /// </summary>
        public void Stop()
        {
            if (!isPlaying) return;

            isPlaying = false;
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }
        }

        /// <summary>
        /// 暂停播放精灵帧动画。
        /// </summary>
        public void Pause()
        {
            if (!isPlaying) return;

            isPlaying = false;
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }
        }

        /// <summary>
        /// 恢复播放精灵帧动画。
        /// </summary>
        public void Resume()
        {
            if (isPlaying) return;

            isPlaying = true;
            playCoroutine = StartCoroutine(PlayFrames());
        }

        /// <summary>
        /// 显示指定索引的帧。
        /// </summary>
        /// <param name="frameIndex">要显示的帧索引。</param>
        public void ShowFrame(int frameIndex)
        {
            if (frames == null || frames.Length == 0) return;

            // 确保索引在有效范围内
            frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
            currentFrame = frameIndex;

            // 显示指定帧
            spriteRenderer.sprite = frames[currentFrame];
        }

        /// <summary>
        /// 显示下一帧。
        /// </summary>
        public void NextFrame()
        {
            if (frames == null || frames.Length == 0) return;

            currentFrame = (currentFrame + 1) % frames.Length;
            spriteRenderer.sprite = frames[currentFrame];
        }

        /// <summary>
        /// 显示上一帧。
        /// </summary>
        public void PreviousFrame()
        {
            if (frames == null || frames.Length == 0) return;

            currentFrame = (currentFrame - 1 + frames.Length) % frames.Length;
            spriteRenderer.sprite = frames[currentFrame];
        }

        /// <summary>
        /// 帧序列播放协程，按设定速度循环播放所有帧。
        /// </summary>
        /// <returns>IEnumerator 用于协程。</returns>
        private IEnumerator PlayFrames()
        {
            while (isPlaying && frames != null && frames.Length > 0)
            {
                // 显示当前帧
                spriteRenderer.sprite = frames[currentFrame];

                // 等待指定时间
                yield return new WaitForSeconds(frameRate);

                // 移动到下一帧
                currentFrame = (currentFrame + 1) % frames.Length;
            }
        }
    }
}
