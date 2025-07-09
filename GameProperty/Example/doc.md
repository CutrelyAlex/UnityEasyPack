# GameProperty ϵͳ�ĵ�

������ GPT-4.1 ���ɣ���ע�����

## 1. Core��GameProperty

### ���
GameProperty �ǻ�������ϵͳ�ĺ��ģ���ʾһ���ɱ����Ρ�����������ֵ���ԡ�֧������������ӷ����˷�����Χ���ƣ��������������������ڽ�ɫ���ԡ�װ�����Եȳ�����

### ��Ҫ����
- **����ֵ��������**����ͨ�� AddModifier ��Ӷ�������������ӷ����˷���Clamp����
- **������ϵ**��֧�����Լ��������Զ������������ĸ�����ѭ��������⡣
- **�¼�����**������ֵ�仯ʱ�ɴ��� OnValueChanged �¼���������Ӧ���Ա����
- **���л�֧��**��֧�� GameProperty �����л��뷴���л�����֧��������Ե�ֱ�����л�����

### ʾ��
```
var hp = new GameProperty("HP", 100f);
hp.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f)); // +20
hp.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f)); // ��1.5
hp.AddModifier(new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 150))); // ���Ʒ�Χ
float value = hp.GetValue(); // �����HP=150
```

### ������ʾ��
```
var strength = new GameProperty("Strength", 10f);
var attack = new GameProperty("Attack", 0f);
attack.AddDependency(strength);
strength.OnValueChanged += (_, __) => {
    attack.SetBaseValue(strength.GetValue() * 2);
};
```

---

## 2. CombineGameProperties���������

### ���
����������ڽ���� GameProperty ���ض���ʽ��ϣ��ʺϸ������Լ��㣨�繥����=����+Buff���ٷֱ�-����ȣ�����Ҫ������ʵ�֣�

- **CombinePropertySingle**����һ���԰�װ
- **CombinePropertyClassic**������Ӽ��˳����
- **CombinePropertyCustom**���Զ�������߼�

### 2.1 CombinePropertySingle
- ������һ�� GameProperty��ֱ�ӷ�����ֵ��
- �ʺ�������ϵļ򵥳�����

**ʾ����**
```
var single = new CombinePropertySingle("SingleProp");
single.ResultHolder.SetBaseValue(50f);
single.ResultHolder.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));
float value = single.GetValue(); // �����60
```

### 2.2 CombinePropertyClassic
- ��϶�� GameProperty���������Buff��Debuff�ȣ�����ͨ�����乫ʽ��������ֵ��
- ��ʽ���������� = (����+�ӳ�) �� (1+�ӳɰٷֱ�) - ���� �� (1+����ٷֱ�)

**ʾ����**
```
var classic = new CombinePropertyClassic(
    "Atk", 100f, "Base", "Buff", "BuffMul", "Debuff", "DebuffMul"
);
classic.GetProperty("Buff").SetBaseValue(30f);
classic.GetProperty("BuffMul").SetBaseValue(0.2f);
classic.GetProperty("Debuff").SetBaseValue(10f);
classic.GetProperty("DebuffMul").SetBaseValue(0.5f);
float value = classic.GetValue(); // ������
```

### 2.3 CombinePropertyCustom
- ֧���Զ�������߼���ͨ��ί�У�Func���������Լ��㷽ʽ��
- �ʺ�������ӵ������������

**ʾ����**
```
var sharedProp = new GameProperty("Shared", 100f);
var combineA = new CombinePropertyCustom("A");
combineA.RegisterProperty(sharedProp);
combineA.Calculater = c => c.GetProperty("Shared").GetValue() + 10;
float valueA = combineA.GetValue(); // 110
```

---

## 3. ���Թ�����

### CombineGamePropertyManager
- �ṩ������Ե�ͳһע�ᡢ��ѯ���������Ƴ��ȹ����ܡ�
- ֧��ͨ�� ID ��ȡ����������������ԡ�

**ʾ����**
```
CombineGamePropertyManager.AddOrUpdate(classic);
var prop = CombineGamePropertyManager.Get("Atk");
```

---

## 4. �����÷���ע������

- ֧�ָ������������¼��������ʺ� RPG�����Եȸ�������ϵͳ��
- ��ֹ��������ѭ������������A->B->A
- ������Ϸʱ��̬����(����Ѫ��)�����ھ�̬����(�������Ѫ����������)����ͨ����������̬�������ԣ�����ֱ���޸Ļ���ֵ��

---

## 5. �ο�ʾ��

��� `GamePropertyExample.cs`�����ǵ����ԡ�������ԡ������������л��ȶ����÷���