
using System;
/// �������ʽ��Property����Ϊ��Ӧ��������������
///     �߻�������Ҫһ�ſ��ƣ����ſ����� ����Buff �� ����Ч�� ���� �����ޣ�
///     �߻�������Ҫһ�ſ��ƣ����ſ����� �˷�����ٷֱ� �� ��ֵ ���� 1% (��������)
/// �����ǣ�
///     �߻�����Ҫһ�����ƣ�������ƿ����� �����˺� ����  2*��2���Ĺ����� + 2 + 50% �ı����˺���*��1+50%��- ������*��1+50%��
///                                                         �ٲ����Թ������� 50% �İٷֱ�
///                                                         ���ر�������
namespace RPGPack
{
    public interface ICombineGameProperty
    {
        string ID { get; }
        GameProperty ResultHolder { get; }
        GameProperty GetProperty(string id);
        Func<ICombineGameProperty, float> Calculater { get; }
        float GetValue();
    }
}