using System.Collections;
using UnityEngine;

public class SpriteFramePlayer : MonoBehaviour
{
    // ����֡����
    public Sprite[] frames;

    // �����ٶȣ�ÿ֡���ʱ�䣬�룩
    public float frameRate = 0.1f;

    // SpriteRenderer�������
    private SpriteRenderer spriteRenderer;

    // ��ǰ֡����
    private int currentFrame = 0;

    // �Ƿ����ڲ���
    private bool isPlaying = false;

    // Э������
    private Coroutine playCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    // ��ʼ����
    public void Play()
    {
        if (isPlaying) return;

        isPlaying = true;
        playCoroutine = StartCoroutine(PlayFrames());
    }

    // ֹͣ����
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

    // ��ͣ����
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

    // �ָ�����
    public void Resume()
    {
        if (isPlaying) return;

        isPlaying = true;
        playCoroutine = StartCoroutine(PlayFrames());
    }

    // ��ʾָ��֡
    public void ShowFrame(int frameIndex)
    {
        if (frames == null || frames.Length == 0) return;

        // ȷ����������Ч��Χ��
        frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        currentFrame = frameIndex;

        // ��ʾָ��֡
        spriteRenderer.sprite = frames[currentFrame];
    }

    // ��ʾ��һ֡
    public void NextFrame()
    {
        if (frames == null || frames.Length == 0) return;

        currentFrame = (currentFrame + 1) % frames.Length;
        spriteRenderer.sprite = frames[currentFrame];
    }

    // ��ʾ��һ֡
    public void PreviousFrame()
    {
        if (frames == null || frames.Length == 0) return;

        currentFrame = (currentFrame - 1 + frames.Length) % frames.Length;
        spriteRenderer.sprite = frames[currentFrame];
    }

    // ֡���в���Э��
    private IEnumerator PlayFrames()
    {
        while (isPlaying && frames != null && frames.Length > 0)
        {
            // ��ʾ��ǰ֡
            spriteRenderer.sprite = frames[currentFrame];

            // �ȴ�ָ��ʱ��
            yield return new WaitForSeconds(frameRate);

            // �ƶ�����һ֡
            currentFrame = (currentFrame + 1) % frames.Length;
        }
    }
}