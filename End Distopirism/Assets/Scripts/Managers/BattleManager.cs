using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    start, 
    playerTurn, 
    enemyTurn, 
    win, 
    lose, 
    pause
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public GameState state;
    public bool enemySelect; //적 선택 여부
    public bool playerSelect; //내 캐릭터 선택 여부
    public int draw = 0; //교착 횟수
    public int playerLeftCnt = 0;
    public int enemyLeftCnt = 0;
    public bool isAllTargetSelected = false; //모든 타겟을 설정했는가
    public bool attacking = false;
    public bool selecting = false;  //적을 선택해야하는 상태일 때

    //다수의 적과 플레이어를 선택할 수 있도록 List 사용
    public List<Player> targetObjs = new List<Player>();
    public List<Player> playerObjs = new List<Player>();

    void Awake()
    {
        state = GameState.start; // 전투 시작 알림
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] enemys = GameObject.FindGameObjectsWithTag("Enemy");
        playerLeftCnt = players.Length;
        enemyLeftCnt = enemys.Length;

        // 게임 시작시 전투 시작
        BattleStart();
    }

    public void Update()
    {
        if (state == GameState.playerTurn && Input.GetMouseButtonDown(0))
        {
            SelectTarget();
            Debug.Log("선택 실행 중");
        }
    }

    private int GetMatchCount() => Mathf.Min(playerObjs.Count, targetObjs.Count);

    void BattleStart()
    {
        // 전투 시작 시 캐릭터 등장 애니메이션 등 효과를 넣고 싶으면 여기 아래에

        // 플레이어나 적에게 턴 넘기기
        // 추후 랜덤 가능성 있음.
        state = GameState.playerTurn;
    }

    //공격&턴 종료 버튼
    public void PlayerAttackButton()
    {
        // 플레이어 턴이 아닐 때 방지
        if (state != GameState.playerTurn || !isAllTargetSelected || attacking)
        {
            Debug.Log("BattleManager.PlayerAttackButton() > 플레이어 턴이 아닙니다.");
            return;
        }

        StartCoroutine(PlayerAttack());
    }

    void SelectTarget()  //내 캐릭터&타겟 선택
    {
        //클릭된 오브젝트 가져오기
        GameObject clickObject = UIManager.Instance.MouseGetObject();

        if (clickObject == null)
            return;

        if (clickObject.CompareTag("Enemy"))
        {
            Player selectedEnemy = clickObject.GetComponent<Player>();

            //동일한 플레이어 클릭 시 선택 취소
            if (targetObjs.Contains(selectedEnemy))
            {
                targetObjs.Remove(selectedEnemy);
                Debug.Log("BattleManager.SelectTarget() > 적 캐릭터 선택 취소됨");
                selecting = true;
            }

            if (selecting)
            {
                //새로운 적 선택
                targetObjs.Add(selectedEnemy);
                Debug.Log("BattleManager.SelectTarget() > 적 캐릭터 선택됨");
                selecting = false;
            }
        }

        // 플레이어 캐릭터 선택 또는 재선택
        if (clickObject.CompareTag("Player"))
        {
            if (selecting)
            {
                Debug.Log("BattleManager.SelectTarget() > 적을 선택해주세요.");
                return;
            }

            Player selectedPlayer = clickObject.GetComponent<Player>();

            //플레이어가 이미 선택된 플레이어 리스트에 있는지 확인
            if (playerObjs.Contains(selectedPlayer))
            {
                //매칭된 적 삭제
                int index = playerObjs.IndexOf(selectedPlayer);
                if (index != -1 && index < targetObjs.Count)
                {
                    targetObjs.RemoveAt(index);
                }

                //동일한 플레이어 클릭 시 선택 취소
                playerObjs.Remove(selectedPlayer);
                Debug.Log("BattleManager.SelectTarget() > 플레이어 캐릭터 선택 취소됨");
            }
            else
            {
                //새로운 플레이어 선택
                playerObjs.Add(selectedPlayer);
                Debug.Log("BattleManager.SelectTarget() > 플레이어 캐릭터 선택됨");
                selecting = true;
            }
        }

        //아군과 적이 모두 선택되었는지 확인
        playerSelect = playerObjs.Count > 0;
        enemySelect = targetObjs.Count > 0;

        isAllTargetSelected = playerObjs.Count == playerLeftCnt && targetObjs.Count == enemyLeftCnt;
    }

    // 공격 레벨과 방어 레벨을 비교하여 보너스 및 패널티 적용
    void DiffCheck()
    {
        int matchCount = GetMatchCount();
        for (int i = 0; i < matchCount; i++)
        {
            Player player = playerObjs[i];
            Player enemy = targetObjs[i];

            player.SetBonusDamage(enemy);
            enemy.SetBonusDamage(player);
        }
    }

    IEnumerator PlayerAttack()  //플레이어 공격턴
    {
        attacking = true;
        yield return new WaitForSeconds(1f);

        Debug.Log("플레이어 공격");
        //공격레벨 방어레벨 대조 for 밖에 있는 이유는 1회만 체크하기 위해. 이미 미리 for 돌림
        DiffCheck();
        //리스트의 각 플레이어와 적이 1:1로 매칭되어 공격
        int matchCount = GetMatchCount();
        for (int i = 0; i < matchCount; i++)
        {
            Player playerObject = playerObjs[i];
            Player targetObject = targetObjs[i];

            //플레이어와 적의 공격력 및 피해 계산

            //코인 리롤
            Debug.Log($"플레이어: {playerObject.Stats.CharName}, 적: {targetObject.Stats.CharName}");
            playerObject.CoinRoll(matchCount);
            targetObject.CoinRoll(matchCount);

            //최종 데미지
            playerObject.CalculateDamage(targetObject);
            targetObject.CalculateDamage(playerObject);
            ShowDamageText(playerObject, targetObject);

            // 합 진행
            // 둘 중 한명이라도 코인이 없다면 바로 피해를 줌
            if (!(playerObject.Stats.Coin > 0 || targetObject.Stats.Coin > 0))
            {
                ApplyDamageNoCoins(playerObject, targetObject);
            }
            else
            {
                //교착 상태 처리 호출
                if (targetObject.Stats.Dmg == playerObject.Stats.Dmg)
                {
                    HandleDraw(ref i, playerObject, targetObject);
                }
                else
                {
                    //승패 처리
                    HandleBattleResult(playerObject, targetObject, ref i);
                }
            }

            //캐릭터들 체력 확인 후 사망 처리
            CheckHealth(playerObject, targetObject);
        }

        //공격 인식 종료
        attacking = false;
    }

    // 데미지 표시 함수
    private void ShowDamageText(Player player, Player target) // FIXME: this should be moved into UIManager
    {
        // 플레이어와 적의 위치 설정
        Vector2 playerPos = player.transform.position;
        Vector2 targetPos = target.transform.position;

        // 더 높은 데미지만 표시
        if (player.Stats.Dmg > target.Stats.Dmg)
        {
            UIManager.Instance.ShowDamageText(player.Stats.Dmg, targetPos + Vector2.up * 2f);
        }
        else
        {
            UIManager.Instance.ShowDamageText(target.Stats.Dmg, playerPos + Vector2.up * 2f);
        }
    }

    // 코인들이 남아있지 않다면
    void ApplyDamageNoCoins(Player playerObject, Player targetObject)
    {
        int matchCount = GetMatchCount();
        if (playerObject.Stats.Coin == 0)
        {
            targetObject.ApplyRemainingDamage(matchCount, playerObject);
        }
        else
        {
            playerObject.ApplyRemainingDamage(matchCount, targetObject);
        }
    }

    //교착 상태 함수
    void HandleDraw(ref int i, Player playerObject, Player targetObject)
    {
        draw++;
        Debug.Log($"교착 상태 발생 {draw} 회");
        
        if (draw < 3)
        {
            i--;
        }

        if (draw >= 3)
        {
            playerObject.Stats.MenTality -= 10;
            targetObject.Stats.MenTality -= 10;
            Debug.Log($"{playerObject.Stats.CharName}과 {targetObject.Stats.CharName} 의 정신력 감소");
            draw = 0;
        }
    }
    
    //재대결
    void HandleBattleResult(Player playerObject, Player targetObject, ref int i)
    {
        Player winner;
        Player loser;
        if(playerObject.Stats.Dmg > targetObject.Stats.Dmg)
        {
            winner = playerObject;
            loser = targetObject;
        }
        else
        {
            winner = targetObject;
            loser = playerObject;
        }

        if (loser.Stats.Coin > 0)
        {
            loser.Stats.Coin--;
            i--;    // 다시 싸우기
        }
        else
        {
            int matchCount = GetMatchCount();
            winner.ApplyRemainingDamage(matchCount, loser);
        }
    }

    // 배틀 시작할 때 인식한 아군&적군의 총 갯수를 체력이 0이하가 됐다면 차감하기.
    void CheckHealth(Player playerObject, Player targetObject)
    {
        if (playerObject.IsDied())
        {
            playerLeftCnt--;
        }
        if (targetObject.IsDied())
        {
            enemyLeftCnt--;
        }
    }

    void CheckBattleEnd()
    {
        if (enemyLeftCnt == 0)
        {
            state = GameState.win;
            Debug.Log("승리");
            EndBattle();
        }
        else if (playerLeftCnt == 0)
        {
            state = GameState.lose;
            Debug.Log("패배");
            EndBattle();
        }
    }

    // 전투 종료
    void EndBattle()
    {
        Debug.Log("전투 종료");
    }

    // 적 공격턴
    IEnumerator EnemyTurn()
    {
        yield return new WaitForSeconds(1f);
        //적 공격 코드
        Debug.Log("적 공격");

        //적 공격 끝났으면 플레이어에게 턴 넘기기
        state = GameState.playerTurn;
        isAllTargetSelected = false;
    }
}
