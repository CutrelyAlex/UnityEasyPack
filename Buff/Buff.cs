using System;

namespace RPGPack
{
    /// <summary>
    /// ��ʾһ��Buff������/����Ч������
    /// �ɸ��ӵ������ϣ�֧�ֳ���ʱ�䡢���ӵȻ��ơ�
    /// </summary>
    public class Buff
    {
        // Buff��Ψһ��ʶ����
        public string BuffID { get; }

        // Buff��Я������������Ӱ�����Եľ��巽ʽ����
        public IModifier Modifier { get; }

        // Buff����ʱ�䣨�룩��-1Ϊ����Buff��
        public float Duration { get; } // -1Ϊ����

        // Buff�Ѵ��ڵ�ʱ�䣨�룩��
        public float Elapsed { get; set; }

        // Buff�Ƿ��ѹ��ڣ�������ʱ����Buff����
        public bool IsExpired => Duration > 0 && Elapsed >= Duration;

        // �Ƿ��������
        public bool CanStack { get; }

        // ��ǰ���Ӳ�����
        public int StackCount { get; set; } = 1;

        // Buff����Դ����������ͬID��ͬ��Դ��Buff���Ƽ�ʹ��string��GameObject�����͡�
        public object Source { get; set; }

        // �����Ӳ�����0Ϊ���޵��ӡ�
        public int MaxStackCount { get; set; } = 0;

        // �����Ƿ���ÿ�γ���ʱ�����ʱ�Ƴ�һ����ӣ�����ֱ���Ƴ�����Buff
        public bool RemoveOneStackEachDuration { get; set; } = false;

        /// <summary>
        /// ����һ��Buffʵ����
        /// </summary>
        /// <param name="buffID">BuffΨһID</param>
        /// <param name="modifier">������</param>
        /// <param name="duration">����ʱ�䣨-1Ϊ���ã�</param>
        /// <param name="canStack">�Ƿ�ɵ���</param>
        /// <param name="source">Buff��Դ</param>
        public Buff(string buffID, IModifier modifier, float duration = -1, bool canStack = false, object source = null)
        {
            BuffID = buffID;
            Modifier = modifier;
            Duration = duration;
            CanStack = canStack;
            Source = source;
        }
    }
}