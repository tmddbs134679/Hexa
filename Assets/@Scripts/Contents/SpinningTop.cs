using UnityEngine;

public class SpinningTop : Puzzle
{
    // ���̴� ��ġ ����, ���� ����, (���ϸ�) �߷� �̵� ���/���� ����
    public override bool IsMatchable => false;        // �� ��ġ ���� ����
    public override bool IsSwappable => true;         // �� �Ϲ� ����ó�� ���� ����
    public override bool AffectedByGravity => true;   // ������ �η��� false�� �ٲ㵵 ��

    [Header("Top Rules")]
    public int requiredStacks = 2;        // 2ȸ ���̸� �ı�
    public int stack = 0;                 // ���� ����
    bool touchedThisRound = false;        // ����� �ߺ� ����
    public bool armedToBreak = false;     // �ı� ���(�ִ� �� ����)

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

    // �̹� ���忡 ����ĭ�� �ϳ��� �����ٸ�(����� 1ȸ��)
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
