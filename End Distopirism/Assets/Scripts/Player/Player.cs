using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerData Stats { get; private set; }

    public bool Live;

    public int bonusdmg = 0;    //diff 차이에 따른 데미지 증가값

    public int coinbonus = 0; //코인 보너스
    public int successCount = 0;  //성공 횟수

    private void Awake()
    {
        Stats = new PlayerData();
        Stats.Init();
    }

    void Start()
    {
        Stats.Coin = Stats.MaxCoin;
        if (Live == false)
        {
            if (Stats.hp > 0)
            {
                Live = true;
            }
        }

        // 스킬을 적용하여 캐릭터의 데미지 값을 설정합니다.
        ApplySkill();
    }

    // 스킬을 캐릭터에 적용하는 메서드
    void ApplySkill()
    {
        if (Stats.skills != null && Stats.skills.Count > 0)
        {
            // 스킬을 적용하여 캐릭터의 데미지 값을 설정하는 예시
            Skill skill = Stats.skills[0]; // 예시로 첫 번째 스킬만 적용
            Stats.MaxDmg = skill.MaxDmg;
            Stats.MinDmg = skill.MinDmg;
            Stats.DmgUp = skill.DmgUp;
            // 필요한 경우 추가로 처리할 코드
            Debug.Log("스킬이 캐릭터에 적용되었습니다: " + skill.skillName);
        }
    }


    // should move below region to seperate class like PlayerHealth
    // and create relationship like PlayerHealth has Player
#region health
    public bool IsDied()
    {
        if (Stats.hp <= 0)
        {
            OnDeath();
            return true;
        }

        return false;
    }

    public void OnDeath()
    {
        gameObject.SetActive(false);
        // todo: 이팩트 재생
    }
#endregion // health

    // should move below region to seperate class like PlayerAttack
    // and create relationship like PlayerAttack has Player
#region attack

    public void SetBonusDamage(Player other)
    {
        if (Stats.DmgLevel < other.Stats.DefLevel + 4)
        {
            bonusdmg = Stats.DmgLevel - Stats.DefLevel / 4;
        }
    }

    public int CalculateDamage(Player other)
    {
        int damage = Random.Range(Stats.MaxDmg, Stats.MinDmg) + coinbonus + bonusdmg;
        Stats.Dmg = damage;
        Debug.Log($"{Stats.CharName}의 최종 데미지: {Stats.Dmg} (기본 데미지: {Stats.MinDmg} - {Stats.MaxDmg}, 코인 보너스: {coinbonus}, 공격 보너스: {bonusdmg})");
        
        return damage;
    }

    public void ApplyRemainingDamage(int rollCount, Player other)
    {
        int coinBonus = 0;

        for (int i = 0; i < Stats.Coin; i++)
        {
            if (i > 0)
            {
                CoinRoll(rollCount);
                Stats.Dmg = Random.Range(Stats.MaxDmg, Stats.MinDmg) + coinBonus + bonusdmg;
            }

            other.Stats.hp -= Stats.Dmg - other.Stats.DefLevel;
            
            if (other.Stats.hp <= 0)
            {
                other.gameObject.SetActive(false);
            }

            // 패배 시 정신력 -2
            other.Stats.MenTality -= 2;
            if (Stats.MenTality < 100)
            {
                // 승리 시 정신력 +1
                Stats.MenTality += 1;
            }
            
            Debug.Log($"{Stats.CharName}이(가) 가한 피해: {Stats.Dmg}");
        }
    }

    public void CoinRoll(int rollCount)
    {
        int initialCoinThrowCount = 0;
        this.successCount = 0;

        for (int i = 0; i < rollCount; i++)
        {
            // 정신력에 따른 확률 계산
            float currentProbability = Mathf.Max(0f, PlayerData.MAX_MENTALITY * (Stats.MenTality / PlayerData.MAX_PROBABILITY));

            for (int j = 0; j < Stats.Coin - 1; j++)
            {
                // 코인 던지기: 현재 확률에 따라 성공 여부 결정
                if (Random.value < currentProbability)
                {
                    this.successCount++;
                }
            }

            coinbonus = initialCoinThrowCount * Stats.DmgUp;
            Debug.Log($"{Stats.CharName}의 코인 던지기 성공 횟수: {initialCoinThrowCount} / {Stats.Coin}");
            Debug.Log($"{Stats.CharName}의 남은 코인: {Stats.Coin} / {Stats.MaxCoin}");
        }
    }

#endregion // attack

}