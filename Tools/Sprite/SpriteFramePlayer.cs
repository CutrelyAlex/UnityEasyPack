using System.Collections;
using UnityEngine;

public class SpriteFramePlayer : MonoBehaviour
{
    // 精灵帧数组
    public Sprite[] frames;

    // 播放速度（每帧间隔时间，秒）
    public float frameRate = 0.1f;

    // SpriteRenderer组件引用
    private SpriteRenderer spriteRenderer;

    // 当前帧索引
    private int currentFrame = 0;

    // 是否正在播放
    private bool isPlaying = false;

    // 协程引用
    private Coroutine playCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    // 开始播放
    public void Play()
    {
        if (isPlaying) return;

        isPlaying = true;
        playCoroutine = StartCoroutine(PlayFrames());
    }

    // 停止播放
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

    // 暂停播放
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

    // 恢复播放
    public void Resume()
    {
        if (isPlaying) return;

        isPlaying = true;
        playCoroutine = StartCoroutine(PlayFrames());
    }

    // 显示指定帧
    public void ShowFrame(int frameIndex)
    {
        if (frames == null || frames.Length == 0) return;

        // 确保索引在有效范围内
        frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        currentFrame = frameIndex;

        // 显示指定帧
        spriteRenderer.sprite = frames[currentFrame];
    }

    // 显示下一帧
    public void NextFrame()
    {
        if (frames == null || frames.Length == 0) return;

        currentFrame = (currentFrame + 1) % frames.Length;
        spriteRenderer.sprite = frames[currentFrame];
    }

    // 显示上一帧
    public void PreviousFrame()
    {
        if (frames == null || frames.Length == 0) return;

        currentFrame = (currentFrame - 1 + frames.Length) % frames.Length;
        spriteRenderer.sprite = frames[currentFrame];
    }

    // 帧序列播放协程
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