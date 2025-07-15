# GameProperty ϵͳʹ��ָ��
** ������ Claude Sonnet 3.7 ���ɣ�ע�����**
## Ŀ¼
- [ϵͳ����](#ϵͳ����)
- [�������](#�������)
- [����ʹ������](#����ʹ������)
  - [������������](#������������)
  - [���������](#���������)
  - [������������](#������������)
- [�������](#�������)
  - [CombinePropertySingle](#combinepropertysingle)
  - [CombinePropertyClassic](#combinepropertyclassic)
  - [CombinePropertyCustom](#combinepropertycustom)
- [���Թ�����](#���Թ�����)
- [���������������](#���������������)
- [�߼��÷�](#�߼��÷�)
  - [����������](#����������)
  - [ѭ���������](#ѭ���������)
  - [������׷��](#������׷��)
  - [�������л�](#�������л�)
- [��Buffϵͳ����](#��buffϵͳ����)
- [���ʵ���������Ż�](#���ʵ���������Ż�)
- [��������](#��������)
  - [��ɫ����ϵͳ](#��ɫ����ϵͳ)
  - [װ���ӳ�ϵͳ](#װ���ӳ�ϵͳ)
  - [����Ч��ϵͳ](#����Ч��ϵͳ)

## ϵͳ����

GamePropertyϵͳ��һ��������Ϸ���Թ����ܣ�רΪRPG�����Ե���Ϸ������ơ����ṩ�˴�����ֵ���Եĸ��ֹ��ܣ�����������Ӧ�á�����������ϵ���¼������ȡ�ϵͳ�����������ƣ�ͨ����ͬ����������������Ϸ�ʽ������ʵ�ָ��ָ��ӵ����Լ����߼���

## �������

- **GameProperty**: ��һ�Ŀ�������ֵ���ԣ�֧����������������ϵ��������׷��
- **CombinePropertyϵ��**: ��϶��GameProperty�Ĳ�ͬʵ�ַ�ʽ
- **CombineGamePropertyManager**: ȫ�����Թ��������������Ե�ע�����ѯ
- **������(IModifier)**: ��������޸�����ֵ�Ľӿڣ��ж��־���ʵ��
- **GamePropertySerializer**: �������Ե����л��뷴���л�

## ����ʹ������

### ������������

```
// ����һ���������ԣ�����ID�ͳ�ʼֵ
var hp = new GameProperty("HP", 100f);

// ��ȡ��������ֵ
float baseValue = hp.GetBaseValue(); // 100

// ���û�������ֵ
hp.SetBaseValue(120f);
```

### ���������

```
// ��Ӽӷ�������������20������ֵ
hp.AddModifier(new FloatModifier(ModifierType.Add, 0, 20f));

// ��ӳ˷�������������50%����ֵ
hp.AddModifier(new FloatModifier(ModifierType.Mul, 0, 1.5f));

// ��ӷ�Χ��������������������ֵ��0-200֮��
hp.AddModifier(new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 200)));

// ��ȡӦ�������������������ֵ
float finalValue = hp.GetValue(); // �����min(180, 200) = 180

// �Ƴ��ض�������
hp.RemoveModifier(someModifier);

// �������������
hp.ClearModifiers();
```

### ������������

```
// ������������
var strength = new GameProperty("Strength", 10f); // ����
var attackPower = new GameProperty("AttackPower", 0f); // ������

// ���������ϵ������������������
attackPower.AddDependency(strength);

// ���������仯ʱ���¹��������߼�
strength.OnValueChanged += (oldVal, newVal) => {
    attackPower.SetBaseValue(strength.GetValue() * 2);
};

// ��ʼ���㹥����
attackPower.SetBaseValue(strength.GetValue() * 2);

// �������仯ʱ�����������Զ�����
strength.SetBaseValue(15f);
float newAttack = attackPower.GetValue(); // 30
```

## �������

����������ڽ����GameProperty���ض���ʽ��ϣ��ṩ�����ֲ�ͬ��ʵ�ַ�ʽ��

### CombinePropertySingle

��򵥵�������ԣ��������ǵ�һGameProperty�İ�װ����

```
// ������һ�������
var single = new CombinePropertySingle("SingleProp");

// ���û���ֵ
single.ResultHolder.SetBaseValue(50f);

// ���������
single.ResultHolder.AddModifier(new FloatModifier(ModifierType.Add, 0, 10f));

// ��ȡ����ֵ
float value = single.GetValue(); // 60
```

### CombinePropertyClassic

�����������Ϸ�ʽ��������RPG��Ϸ�г��������Լ��㹫ʽ��

��ʽ���������� = (����+�ӳ�) �� (1+�ӳɰٷֱ�) - ���� �� (1+����ٷֱ�)

```
// ���������������
var classic = new CombinePropertyClassic(
    "AttackPower", // ID
    50f,           // ��ʼ����ֵ
    "Base",        // ����������
    "Buff",        // Buff������
    "BuffMul",     // Buff�ٷֱ���
    "Debuff",      // Debuff������
    "DebuffMul"    // Debuff�ٷֱ���
);

// ���ø�����ֵ
classic.GetProperty("Base").AddModifier(new FloatModifier(ModifierType.Add, 0, 20f)); // �����ӳ�
classic.GetProperty("Buff").SetBaseValue(10f);  // Buff�ӳ�
classic.GetProperty("BuffMul").SetBaseValue(0.2f);  // Buff�ٷֱ�(+20%)
classic.GetProperty("Debuff").SetBaseValue(5f);  // Debuff����
classic.GetProperty("DebuffMul").SetBaseValue(0.5f);  // Debuff�ٷֱ�(+50%)

// ��������ֵ: (50+20+10)*(1+0.2) - 5*(1+0.5) = 80*1.2 - 5*1.5 = 96 - 7.5 = 88.5
float finalAttack = classic.GetValue(); // 88.5
```

### CombinePropertyCustom

��ȫ�Զ������Ϸ�ʽ��ͨ��ί�к���������������߼���

```
// ����������Ļ�������
var sharedProp = new GameProperty("Shared", 100f);

// �����Զ����������A
var combineA = new CombinePropertyCustom("A");
combineA.RegisterProperty(sharedProp);
combineA.Calculater = c => c.GetProperty("Shared").GetValue() + 10;

// �����Զ����������B
var combineB = new CombinePropertyCustom("B");
combineB.RegisterProperty(sharedProp);
combineB.Calculater = c => c.GetProperty("Shared").GetValue() * 2;

// ��ȡ���Եļ�����
float valueA = combineA.GetValue(); // 110
float valueB = combineB.GetValue(); // 200

// �޸Ĺ������Ժ�����������Զ�����Ӧ����
sharedProp.SetBaseValue(50f);
valueA = combineA.GetValue(); // 60
valueB = combineB.GetValue(); // 100
```

## ���Թ�����

CombineGamePropertyManager�ṩ��ȫ�ֹ���������ԵĹ��ܡ�

```
// ע���������
CombineGamePropertyManager.AddOrUpdate(classic);
CombineGamePropertyManager.AddOrUpdate(single);

// ͨ��ID��ȡ�������
var prop = CombineGamePropertyManager.Get("AttackPower");

// ��������ע����������
foreach (var p in CombineGamePropertyManager.GetAll())
{
    Debug.Log($"����ID: {p.ID}, ��ǰֵ: {p.GetValue()}");
}

// �Ƴ��������
CombineGamePropertyManager.Remove("SingleProp");
```

## ���������������

GamePropertyϵͳ֧�ֶ������������ͣ�ÿ���������ض���Ӧ�ò��ԣ�

1. **Add**: ֱ�����ֵ
2. **PriorityAdd**: �����ȼ����ֵ
3. **Mul**: ֱ�ӳ���ֵ
4. **PriorityMul**: �����ȼ�����ֵ
5. **AfterAdd**: �ڳ˷����κ������ֵ
6. **Override**: ֱ�Ӹ�������ֵ
7. **Clamp**: ��������ֵ��Χ

```
// ������ͬ���͵�������
var addMod = new FloatModifier(ModifierType.Add, 0, 50f);  // +50
var mulMod = new FloatModifier(ModifierType.Mul, 0, 1.5f); // ��1.5
var clampMod = new RangeModifier(ModifierType.Clamp, 0, new Vector2(0, 200)); // ���Ʒ�Χ0-200
var overrideMod = new FloatModifier(ModifierType.Override, 0, 100f); // ֱ����Ϊ100

// ���������ȼ�Ӱ��Ӧ��˳��
var highPriorityAdd = new FloatModifier(ModifierType.Add, 10, 20f); // �����ȼ�����Ӧ��
var lowPriorityAdd = new FloatModifier(ModifierType.Add, 0, 10f);  // �����ȼ�����Ӧ��
```

## �߼��÷�

### ����������

GameProperty֧�ֹ������ӵ�����������������ʵ��RPG��Ϸ�е����Թ������㡣

```
// ������������
var strength = new GameProperty("Strength", 10f); // ����
var agility = new GameProperty("Agility", 8f);    // ����
var intelligence = new GameProperty("Intelligence", 12f); // ����

// ������������
var attackPower = new GameProperty("AttackPower", 0f); // ������ = ����*2 + ����*0.5
var attackSpeed = new GameProperty("AttackSpeed", 0f); // �����ٶ� = ����*0.1 + 1
var spellPower = new GameProperty("SpellPower", 0f);   // ����ǿ�� = ����*3

// ����������ϵ
attackPower.AddDependency(strength);
attackPower.AddDependency(agility);
attackSpeed.AddDependency(agility);
spellPower.AddDependency(intelligence);

// ���ü����߼�
strength.OnValueChanged += (_, __) => {
    attackPower.SetBaseValue(strength.GetValue() * 2 + agility.GetValue() * 0.5f);
};

agility.OnValueChanged += (_, __) => {
    attackPower.SetBaseValue(strength.GetValue() * 2 + agility.GetValue() * 0.5f);
    attackSpeed.SetBaseValue(agility.GetValue() * 0.1f + 1f);
};

intelligence.OnValueChanged += (_, __) => {
    spellPower.SetBaseValue(intelligence.GetValue() * 3);
};

// �����������ԣ�DPS(ÿ���˺�) = ������ * �����ٶ�
var dps = new GameProperty("DPS", 0f); 
dps.AddDependency(attackPower);
dps.AddDependency(attackSpeed);

attackPower.OnValueChanged += (_, __) => {
    dps.SetBaseValue(attackPower.GetValue() * attackSpeed.GetValue());
};

attackSpeed.OnValueChanged += (_, __) => {
    dps.SetBaseValue(attackPower.GetValue() * attackSpeed.GetValue());
};
```

### ѭ���������

ϵͳ�Զ���Ⲣ��ֹѭ���������������޵ݹ���ɵı�����

```
var propA = new GameProperty("A", 10f);
var propB = new GameProperty("B", 20f);

// ����������ϵ: A -> B
propA.AddDependency(propB);

// ���Խ���ѭ������: B -> A���ᱻϵͳ��ֹ��
propB.AddDependency(propA); // ������Ч������̨���������
```

### ������׷��

GamePropertyͨ�����ǻ��ƣ����ⲻ��Ҫ���ظ����㣬������ܡ�

```
// �������Ա����Ϊ����¼�
property.OnDirty(() => {
    Debug.Log("������Ҫ���¼���");
});

// �ֶ������Ա��Ϊ�ࣨͨ������Ҫ�ֶ����ã�
property.MakeDirty();

// �Ƴ������ݼ���
property.RemoveOnDirty(someAction);
```

### �������л�

GameProperty֧�����л��뷴���л������ڴ浵�ͼ��ء�

```
// ������������������
var prop = new GameProperty("MP", 80f);
prop.AddModifier(new FloatModifier(ModifierType.Add, 1, 10f));
prop.AddModifier(new FloatModifier(ModifierType.Mul, 2, 2f));

// ���л�
var serialized = GamePropertySerializer.Serialize(prop);

// �����л�
var deserialized = GamePropertySerializer.FromSerializable(serialized);

// ��ֵ֤�Ƿ�һ��
float originalValue = prop.GetValue();
float deserializedValue = deserialized.GetValue();
```

## ��Buffϵͳ����

GamePropertyϵͳ������Buffϵͳ�޷켯�ɣ�ʵ�����ԵĶ�̬�޸ġ�

```
// ����һ���޸��������Ե�Buff
var buffData = new BuffData
{
    ID = "Buff_Strength",
    Name = "��������",
    Description = "���ӽ�ɫ����������",
    Duration = 10f  // ����10��
};

// ����һ���޸��������Ե����η�
var strengthModifier = new FloatModifier(ModifierType.Add, 0, 5f);  // ����5������

// ����Module����ӵ�BuffData
var propertyModule = new CastModifierToProperty(strengthModifier, "Strength");
buffData.BuffModules.Add(propertyModule);

// ͨ��BuffManagerӦ�����Buff
buffManager.AddBuff(buffData, caster, target);
```

## ���ʵ���������Ż�

1. **����ʹ���������**�����ڸ������Լ��㣬����ʹ��CombineProperty����ֱ��ʹ��GameProperty��

2. **����Ƶ���޸Ļ���ֵ**�����ھ�̬���ԣ����������ֵ����������������Ӧͨ����������̬����������ֱ���޸Ļ���ֵ��

3. **�����������������ȼ�**����������Ӧ��˳�����Ӱ�����ս�����ر����ڻ��ʹ�ò�ͬ���͵�������ʱ��

4. **������������**����Ȼϵͳ֧�ָ��ӵ������������������ȸ��ӵ�������ϵ���ܵ���ά�����Ѻ��������⡣

5. **�������ǻ���**��ϵͳ���õ����ǻ��ƿ��Ա��ⲻ��Ҫ���ظ����㣬������ܡ�

## ��������

### ��ɫ����ϵͳ

```
// ������������
var strength = new GameProperty("Strength", 10f);
var agility = new GameProperty("Agility", 8f);
var intelligence = new GameProperty("Intelligence", 12f);

// ������������
var health = new CombinePropertyCustom("Health");
health.RegisterProperty(strength);
health.Calculater = c => c.GetProperty("Strength").GetValue() * 10;

var mana = new CombinePropertyCustom("Mana");
mana.RegisterProperty(intelligence);
mana.Calculater = c => c.GetProperty("Intelligence").GetValue() * 10;

// ע�ᵽȫ�ֹ�����
CombineGamePropertyManager.AddOrUpdate(health);
CombineGamePropertyManager.AddOrUpdate(mana);
```

### װ���ӳ�ϵͳ
���������ο�
```
// ��ɫ��������
var baseStrength = new GameProperty("BaseStrength", 10f);

// ��������������Լ���������
var totalStrength = new CombinePropertyClassic(
    "TotalStrength", baseStrength.GetValue(), "Base", "Equipment", "EquipmentMul", "Debuff", "DebuffMul"
);

// װ���ṩ�������ӳ�
void EquipItem(Item item)
{
    // ����item.StrengthBonus��װ���ṩ�������ӳ�
    totalStrength.GetProperty("Equipment").AddModifier(
        new FloatModifier(ModifierType.Add, item.Priority, item.StrengthBonus)
    );
    
    // ˢ����ʾ
    UpdateUI();
}

// ж��װ��
void UnequipItem(Item item)
{
    // �Ƴ�װ���ṩ�ļӳ�
    totalStrength.GetProperty("Equipment").RemoveModifier(
        new FloatModifier(ModifierType.Add, item.Priority, item.StrengthBonus)
    );
    
    // ˢ����ʾ
    UpdateUI();
}
```

### ����Ч��ϵͳ
���������ο���ʵ�ʵļ���ϵͳ����Ӹ���
```
// ���弼��Ч��
void ApplyFireballEffect(Character target)
{
    // ��ȡĿ���ħ������
    var magicResist = CombineGamePropertyManager.Get("MagicResist");
    float resistValue = magicResist != null ? magicResist.GetValue() : 0;
    
    // ��ȡʩ���ߵķ���ǿ��
    var spellPower = CombineGamePropertyManager.Get("SpellPower");
    float spellPowerValue = spellPower != null ? spellPower.GetValue() : 0;
    
    // �����˺�
    float baseDamage = 50;
    float finalDamage = baseDamage + spellPowerValue * 0.8f;
    finalDamage *= (1 - resistValue / 100);
    
    // Ӧ���˺�
    target.TakeDamage(finalDamage);
    
    // �������Ч��Buff
    var burnBuff = new BuffData { ID = "Burn", Duration = 3f };
    burnBuff.BuffModules.Add(new DamageOverTimeModule(finalDamage * 0.1f));
    buffManager.AddBuff(burnBuff, caster, target);
}
```

---

ͨ���������GamePropertyϵͳ�ĸ��ֹ��ܣ����Թ��������Ӷ�������Ϸ����ϵͳ�����㲻ͬ������Ϸ������ϵͳ��ģ�黯���ʹ��ͬ�������߼����Է��벢�ظ�ʹ�ã�������չ��ά����
