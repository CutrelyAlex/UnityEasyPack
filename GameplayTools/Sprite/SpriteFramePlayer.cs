using System.Collections;
using UnityEngine;

/// <summary>
/// SpriteFramePlayer ������ Unity �в��ž���֡������
/// ֧�ֲ��š���ͣ��ֹͣ���ָ����л�֡�Ȳ�����
/// </summary>
public class SpriteFramePlayer : MonoBehaviour
{
    /// <summary>
    /// ����֡���顣
    /// </summary>
    public Sprite[] frames;

    /// <summary>
    /// �����ٶȣ�ÿ֡���ʱ�䣬��λ���룩��
    /// </summary>
    public float frameRate = 0.1f;

    /// <summary>
    /// SpriteRenderer ������á�
    /// </summary>
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// ��ǰ֡������
    /// </summary>
    private int currentFrame = 0;

    /// <summary>
    /// �Ƿ����ڲ��š�
    /// </summary>
    private bool isPlaying = false;

    /// <summary>
    /// ����Э�̵����á�
    /// </summary>
    private Coroutine playCoroutine;

    /// <summary>
    /// ��ʼ���������ȡ����� SpriteRenderer��
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
    /// ��ʼ���ž���֡������
    /// </summary>
    public void Play()
    {
        if (isPlaying) return;

        isPlaying = true;
        playCoroutine = StartCoroutine(PlayFrames());
    }

    /// <summary>
    /// ֹͣ���ž���֡������
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
    /// ��ͣ���ž���֡������
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
    /// �ָ����ž���֡������
    /// </summary>
    public void Resume()
    {
        if (isPlaying) return;

        isPlaying = true;
        playCoroutine = StartCoroutine(PlayFrames());
    }

    /// <summary>
    /// ��ʾָ��������֡��
    /// </summary>
    /// <param name="frameIndex">Ҫ��ʾ��֡������</param>
    public void ShowFrame(int frameIndex)
    {
        if (frames == null || frames.Length == 0) return;

        // ȷ����������Ч��Χ��
        frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        currentFrame = frameIndex;

        // ��ʾָ��֡
        spriteRenderer.sprite = frames[currentFrame];
    }

    /// <summary>
    /// ��ʾ��һ֡��
    /// </summary>
    public void NextFrame()
    {
        if (frames == null || frames.Length == 0) return;

        currentFrame = (currentFrame + 1) % frames.Length;
        spriteRenderer.sprite = frames[currentFrame];
    }

    /// <summary>
    /// ��ʾ��һ֡��
    /// </summary>
    public void PreviousFrame()
    {
        if (frames == null || frames.Length == 0) return;

        currentFrame = (currentFrame - 1 + frames.Length) % frames.Length;
        spriteRenderer.sprite = frames[currentFrame];
    }

    /// <summary>
    /// ֡���в���Э�̣����趨�ٶ�ѭ����������֡��
    /// </summary>
    /// <returns>IEnumerator ����Э�̡�</returns>
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