using UnityEngine;

public class SpinningTop : Puzzle
{
    // 팽이는 매치 불참, 스왑 가능, (원하면) 중력 이동 허용/금지 선택
    public override bool IsMatchable => false;        // ★ 매치 절대 불참
    public override bool IsSwappable => true;         // ★ 일반 퍼즐처럼 스왑 가능
    public override bool AffectedByGravity => true;   // 가만히 두려면 false로 바꿔도 됨

    [Header("Top Rules")]
    public int requiredStacks = 2;        // 2회 쌓이면 파괴
    public int stack = 0;                 // 현재 스택
    bool touchedThisRound = false;        // 라운드당 중복 방지
    public bool armedToBreak = false;     // 파괴 대기(애니 후 제거)

    [Header("Anim")]
    public Animator animator;
    private readonly int IdleHas = Animator.StringToHash("Idle");
    private readonly int SpinningHas = Animator.StringToHash("Spinning");
    private readonly int BreakHas = Animator.StringToHash("Break");

    
    public void Awake()
    {
       if(animator != null)
            animator.Play(IdleHas);
    }
    public void RoundReset()
    {
        touchedThisRound = false;
    }

    // 이번 라운드에 인접칸이 하나라도 깨졌다면(라운드당 1회만)
    public void OnAdjacentDestroyedThisRound()
    {
        if (armedToBreak || touchedThisRound) return;
        touchedThisRound = true;

        stack = Mathf.Min(requiredStacks, stack + 1);

        if (stack == 1 && animator)
            animator.Play(SpinningHas);

        if (stack >= requiredStacks)
        {
            armedToBreak = true;
            if (animator) animator.Play(BreakHas);
        }
    }
}
