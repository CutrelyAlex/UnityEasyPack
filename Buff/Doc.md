# Buffϵͳʹ��ָ��

## Ŀ¼
- [ϵͳ����](#ϵͳ����)
- [�������](#�������)
- [����ʹ������](#����ʹ������)
- [Buff����������](#buff����������)
- [Buff�ĵ��Ӻͳ���ʱ�����](#buff�ĵ��Ӻͳ���ʱ�����)
- [Modules����ָ��](#modules����ָ��)
  - [�����Զ���Module](#�����Զ���module)
  - [Module�ص�����](#module�ص�����)
  - [�Զ���ص�](#�Զ���ص�)
  - [���ȼ�����](#���ȼ�����)
- [��������](#��������)
  - [�����޸���Buff](#�����޸���buff)
  - [��ʱ����Ч��](#��ʱ����Ч��)
  - [���ѵ�Ч��](#���ѵ�Ч��)

## ϵͳ����

Buffϵͳ��һ������״̬Ч�������ܣ����ڴ�����Ϸ�еĸ�����ʱ״̬Ч���������桢����ȣ���ϵͳ����ģ�黯��ƣ�����ͨ����ϲ�ͬ��Module��ʵ�ָ��ָ���Ч����

## �������

- **BuffManager**: ����Buff���������ڹ�����¼�����
- **Buff**: ����Buff��ʵ��������BuffData������ʱ״̬
- **BuffData**: Buff�ľ�̬��������
- **BuffModule**: ����Buff��Ϊ��ģ�����
- **���־���Module**: ��`CastModifierToProperty`�����޸���Ϸ����

## ����ʹ������

### 1. ����BuffData

```csharp
// ����BuffData
var buffData = new BuffData
{
    ID = "Buff_Strength",
    Name = "��������",
    Description = "���ӽ�ɫ����������",
    Duration = 10f,                  // ����10��
    TriggerInterval = 1f,            // ÿ�봥��һ��
    MaxStacks = 3,                   // ������3��
    BuffSuperpositionStrategy = BuffSuperpositionDurationType.Add,  // ����ʱ���ӳ���ʱ��
    BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,  // �������Ӳ���
    TriggerOnCreate = true           // ����ʱ��������һ��
};

// ��ӱ�ǩ�Ͳ㼶�������ڷ������
buffData.Tags.Add("Positive");      // ����Ч����ǩ
buffData.Layers.Add("Attribute");   // ���Բ㼶
```

### 2. ���Module

```csharp
// ����һ���޸��������Ե����η�
var strengthModifier = new FloatModifier(ModifierType.Add, 0, 5f);  // ����5������

// ����Module����ӵ�BuffData
var propertyModule = new CastModifierToProperty(strengthModifier, "Strength");
buffData.BuffModules.Add(propertyModule);
```

### 3. ����BuffManager��Ӧ��Buff

```csharp
// ����BuffManager
var buffManager = new BuffManager();

// Ӧ��Buff��Ŀ��
GameObject caster = ...; // Buff�Ĵ�����
GameObject target = ...; // Buff��Ŀ��
var buff = buffManager.AddBuff(buffData, caster, target);

// ����Ϸѭ���и���BuffManager
void Update()
{
    buffManager.Update(Time.deltaTime);
}
```

### 4. ����Buff

```csharp
// �Ƴ��ض�Buff
buffManager.RemoveBuff(buff);

// �Ƴ�Ŀ���ϵ�����Buff
buffManager.RemoveAllBuffs(target);

// �Ƴ�Ŀ�����ض�ID��Buff
buffManager.RemoveBuffByID(target, "Buff_Strength");

// �Ƴ�Ŀ���ϴ����ض���ǩ��Buff
buffManager.RemoveBuffsByTag(target, "Positive");

// ���Ŀ���Ƿ����ض�Buff
bool hasBuff = buffManager.HasBuff(target, "Buff_Strength");

// ��ȡĿ���ϵ�����Buff
List<Buff> allBuffs = buffManager.GetAllBuffs(target);
```

## Buff����������

Buff�������������лᴥ�������¼���

1. **OnCreate**: Buff������ʱ
2. **OnTrigger**: Buff��TriggerInterval��ʱ����ʱ
3. **OnUpdate**: ÿ֡����ʱ
4. **OnAddStack**: Buff�ѵ�����ʱ
5. **OnReduceStack**: Buff�ѵ�����ʱ
6. **OnRemove**: Buff���Ƴ�ʱ

## Buff�ĵ��Ӻͳ���ʱ�����

### ����ʱ����� (BuffSuperpositionDurationType)

- **Add**: ���ӳ���ʱ��
- **ResetThenAdd**: ���ó���ʱ����ٵ���
- **Reset**: ���ó���ʱ��
- **Keep**: ����ԭ�г���ʱ�䲻��

### �ѵ������� (BuffSuperpositionStacksType)

- **Add**: ���Ӷѵ���
- **ResetThenAdd**: ���öѵ������ٵ���
- **Reset**: ���öѵ���
- **Keep**: ����ԭ�жѵ�������

### �Ƴ����� (BuffRemoveType)

- **All**: ��ȫ�Ƴ�Buff
- **OneStack**: ����һ��ѵ�
- **Manual**: ���Զ��Ƴ������ֶ�����

## Modules����ָ��

### �����Զ���Module

�����Զ���Module��Ҫ�̳�`BuffModule`���࣬��ʵ����صĻص�����

```csharp
public class MyCustomBuffModule : BuffModule
{
    public MyCustomBuffModule()
    {
        // ע����ض��ص����͸���Ȥ
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
        RegisterCallback(BuffCallBackType.OnRemove, OnRemove);
        RegisterCallback(BuffCallBackType.OnTick, OnTick);
    }

    private void OnCreate(Buff buff, object[] parameters)
    {
        // Buff����ʱ���߼�
        Debug.Log($"Buff {buff.BuffData.Name} �Ѵ���!");
        
        // ���Է���buff�ĸ�������
        GameObject target = buff.Target;
        int currentStacks = buff.CurrentStacks;
        
        // ִ���Զ����߼�...
    }

    private void OnRemove(Buff buff, object[] parameters)
    {
        // Buff�Ƴ�ʱ���߼�
        Debug.Log($"Buff {buff.BuffData.Name} ���Ƴ�!");
        
        // ������Դ��״̬...
    }
    
    private void OnTick(Buff buff, object[] parameters)
    {
        // Buff��ʱ����ʱ���߼�
        Debug.Log($"Buff {buff.BuffData.Name} ����Ч��!");
        
        // ���磺ÿ�δ�������˺�
        // DamageSystem.ApplyDamage(buff.Target, 10f);
    }
}
```

### Module�ص�����

`BuffCallBackType`ö�ٶ��������»ص����ͣ�

- **OnCreate**: Buff����ʱ
- **OnRemove**: Buff�Ƴ�ʱ
- **OnAddStack**: Buff�ѵ�����ʱ
- **OnReduceStack**: Buff�ѵ�����ʱ
- **OnUpdate**: ÿ֡����ʱ
- **OnTick**: Buff���������ʱ
- **Custom**: �Զ���ص�

### �Զ���ص�

���˱�׼�ص��⣬������ע���Զ���ص���

```csharp
public class AdvancedBuffModule : BuffModule
{
    public AdvancedBuffModule()
    {
        // ע���׼�ص�
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
        
        // ע���Զ���ص�
        RegisterCustomCallback("OnTargetDamaged", OnTargetDamaged);
        RegisterCustomCallback("OnSkillCast", OnSkillCast);
    }
    
    private void OnCreate(Buff buff, object[] parameters)
    {
        // ���洴���߼�
    }
    
    private void OnTargetDamaged(Buff buff, object[] parameters)
    {
        // ��Ŀ������ʱ�����⴦��
        float damageAmount = (float)parameters[0];
        Debug.Log($"Buff��Ӧ�˺��¼�: {damageAmount}");
        
        // ����Ч��...
    }
    
    private void OnSkillCast(Buff buff, object[] parameters)
    {
        // ������ʩ��ʱ�����⴦��
        string skillId = (string)parameters[0];
        Debug.Log($"Buff��Ӧ����ʩ��: {skillId}");
        
        // ����Ч��...
    }
}
```

����Ϸ�����д����Զ���ص���

```csharp
// �ں��ʵ�λ�ô����Զ���ص�
buffManager.ExecuteBuffModules(buff, BuffCallBackType.Custom, "OnTargetDamaged", damageAmount);
```

### ���ȼ�����

��������Module�����ȼ������ƶ��Module��ִ��˳��

```csharp
public class HighPriorityModule : BuffModule
{
    public HighPriorityModule()
    {
        // ���ø����ȼ����������ڵ����ȼ�ģ��ִ��
        Priority = 100;
        
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
    }
    
    private void OnCreate(Buff buff, object[] parameters)
    {
        // ��ִ�е��߼�
        Debug.Log("�����ȼ�ģ��ִ��");
    }
}

public class LowPriorityModule : BuffModule
{
    public LowPriorityModule()
    {
        // ���õ����ȼ���������ڸ����ȼ�ģ��ִ��
        Priority = 0;
        
        RegisterCallback(BuffCallBackType.OnCreate, OnCreate);
    }
    
    private void OnCreate(Buff buff, object[] parameters)
    {
        // ��ִ�е��߼�
        Debug.Log("�����ȼ�ģ��ִ��");
    }
}
```

## ��������

### �����޸���Buff

ʹ��`CastModifierToProperty`ģ���޸Ľ�ɫ���ԣ�

```csharp
// ���������ƶ��ٶ�20%��Buff
var speedBuff = new BuffData
{
    ID = "Speed_Boost",
    Name = "����",
    Duration = 5f
};

// �����˷����η�������20%��
var speedModifier = new FloatModifier(ModifierType.Mul, 1, 0.2f);

// ���������Module
var speedModule = new CastModifierToProperty(speedModifier, "MovementSpeed");
speedBuff.BuffModules.Add(speedModule);

// Ӧ��Buff
buffManager.AddBuff(speedBuff, caster, target);
```

### ��ʱ����Ч��

������ʱ����Ч����Buff��������˺�����

```csharp
// ���������˺�Buff
var dotBuff = new BuffData
{
    ID = "Poison",
    Name = "�ж�",
    Duration = 10f,
    TriggerInterval = 1f,  // ÿ�봥��һ��
};

// �����Զ���Module�����˺��߼�
public class DamageOverTimeModule : BuffModule
{
    private float _damagePerTick;
    
    public DamageOverTimeModule(float damagePerTick)
    {
        _damagePerTick = damagePerTick;
        RegisterCallback(BuffCallBackType.OnTick, OnTick);
    }
    
    private void OnTick(Buff buff, object[] parameters)
    {
        // ����˺�
        var target = buff.Target.GetComponent<Health>();
        if (target != null)
        {
            target.TakeDamage(_damagePerTick);
        }
    }
}

// ���Module
dotBuff.BuffModules.Add(new DamageOverTimeModule(5f));  // ÿ�����5���˺�
```

### ���ѵ�Ч��

����Ч����ѵ��������ӵ�Buff��

```csharp
// �����ɶѵ��Ĺ�����Buff
var stackableBuff = new BuffData
{
    ID = "Rage",
    Name = "ŭ��",
    MaxStacks = 5,  // ���5��
    Duration = 8f,
    BuffSuperpositionStrategy = BuffSuperpositionDurationType.Reset,  // ���ó���ʱ��
    BuffSuperpositionStacksStrategy = BuffSuperpositionStacksType.Add,  // ���Ӳ���
};

// ��������ݶѵ�������Ч����ģ��
public class StackableEffectModule : BuffModule
{
    private float _baseEffect;
    
    public StackableEffectModule(float baseEffect)
    {
        _baseEffect = baseEffect;
        RegisterCallback(BuffCallBackType.OnCreate, ApplyEffect);
        RegisterCallback(BuffCallBackType.OnAddStack, ApplyEffect);
        RegisterCallback(BuffCallBackType.OnReduceStack, ApplyEffect);
        RegisterCallback(BuffCallBackType.OnRemove, RemoveEffect);
    }
    
    private void ApplyEffect(Buff buff, object[] parameters)
    {
        // �Ƴ���Ч��
        RemoveEffect(buff, parameters);
        
        // ���㵱ǰЧ��ֵ
        float currentEffect = _baseEffect * buff.CurrentStacks;
        
        // Ӧ��Ч��
        var attackPower = CombineGamePropertyManager.GetGameProperty("AttackPower");
        if (attackPower != null)
        {
            var modifier = new FloatModifier(ModifierType.Add, 0, currentEffect);
            attackPower.AddModifier(modifier);
            
            // �洢modifier�Ա�����Ƴ�
            buff.BuffData.CustomData["CurrentModifier"] = modifier;
        }
    }
    
    private void RemoveEffect(Buff buff, object[] parameters)
    {
        var attackPower = CombineGamePropertyManager.GetGameProperty("AttackPower");
        if (attackPower != null && buff.BuffData.CustomData.TryGetValue("CurrentModifier", out object modObj))
        {
            var modifier = modObj as IModifier;
            attackPower.RemoveModifier(modifier);
        }
    }
}

// ���Module
stackableBuff.BuffModules.Add(new StackableEffectModule(5f));  // ÿ������5�㹥����
```

---

ͨ���������BuffData��Module�����Դ������ָ��ӵ���ϷЧ����Buffϵͳ��ģ�黯���ʹ�ò�ͬ��Ч���߼����Է��벢�ظ�ʹ�ã�������չ��ά����