using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class Candy : MonoBehaviour
{

    public List<Sprite> sprites;

    public int spriteIndex;
    public float moveSpeed = 0.8f;

    private static Candy selected;

    private SpriteRenderer render;

    public bool special = false;
    public int s_direction;
    public int row = -1;
    public int column = -1;
    public bool moveDone = true;

    void Start() // 첫 생성시 블록 랜덤하게 생성
    {
        render = GetComponent<SpriteRenderer>();
        spriteIndex = Random.Range(0, sprites.Count - 1);
        render.sprite = sprites[spriteIndex];
    }

    void Update()
    {
        if (special)
        {
            render = GetComponent<SpriteRenderer>();
            spriteIndex = 4;
            render.sprite = sprites[spriteIndex];
        }
    }

    public void SetRowColumn(int row, int col)
    {
        this.row = row;
        this.column = col;
    }


    public void Select()
    {
        render.color = Color.grey;
    }


    public void UnSelect()
    {
        render.color = Color.white;
    }


    private void OnMouseDown()
    {
        if(!GridManager.instance.specialdestroy) // 먼치킨 블록을 굴리고 있으면 선택 못함
        {
            if (selected == this)
            {
                selected = null;
                UnSelect();
                return;
            }
            if (selected != null)
            {
                selected.UnSelect();
                if (Vector3.Distance(selected.transform.position, transform.position) == 1)
                {
                    SwapAndCheckMatch(selected, this, false);
                    selected = null;
                    return;
                }
            }
            selected = this;
            Select();
        }

    }


    NormalCallBack FillMoveCB;
    public void MoveToBlank(int row, int col, NormalCallBack cb) // 이동 함수
    {
        FillMoveCB = cb; // 콜백 사용
        moveDone = false;
        transform.DOMove(GridManager.instance.GridIndexToPos(row, col), moveSpeed) // 해당 위치로 이동 DoTween API 사용
            .OnComplete(MoveToBlankDone);

        this.row = row;
        this.column = col;

        if (GridManager.instance.Grid[row, column] == this.gameObject)
            GridManager.instance.Grid[row, column] = null;
        GridManager.instance.Grid[row, column] = this.gameObject;
    }
    void MoveToBlankDone()
    {
        moveDone = true;
        FillMoveCB?.Invoke();
    }


    Candy swap_a;
    Candy swap_b;
    public void SwapAndCheckMatch(Candy a, Candy b, bool forReturn) //forReturn : 매치된게 없으면 복귀, 블록 바꾸기 함수
    {
        swap_a = a;
        swap_b = b;

        Sequence seq = DOTween.Sequence();
        //위치 바꾸기
        int t_row = a.row;
        int t_col = a.column;
        a.GetComponent<Candy>().SetRowColumn(b.row, b.column);
        b.GetComponent<Candy>().SetRowColumn(t_row, t_col);
        if(a.special)
        {
            if (a.row == b.row - 1)
            {
                Debug.Log("아래");
                a.s_direction = 2;
            }
            if (a.row == b.row + 1)
            {
                Debug.Log("위");
                a.s_direction = 1;
            }
            if (a.column == b.column + 1)
            {
                Debug.Log("오른쪽");
                a.s_direction = 4;
            }
            if (a.column == b.column - 1)
            {
                Debug.Log("왼쪽");
                a.s_direction = 3;
            }
            // 먼치킨 블록이면 원래 위치로 되돌리고 굴림
            t_row = a.row;
            t_col = a.column;
            a.GetComponent<Candy>().SetRowColumn(b.row, b.column);
            b.GetComponent<Candy>().SetRowColumn(t_row, t_col);

            seq.Append(a.transform.DOMove(a.transform.position, moveSpeed))
                .OnComplete(forReturn ? (TweenCallback)null : SwapMOveCompleted);
            GridManager.instance.specialdestroy = true;
        }
        else
        { // DOTween API 사용
            seq.Append(a.transform.DOMove(b.transform.position, moveSpeed))
                .Join(b.transform.DOMove(a.transform.position, moveSpeed))
                .OnComplete(forReturn ? (TweenCallback)null : SwapMOveCompleted);
        }
        GridManager.instance.Grid[a.row, a.column] = a.gameObject;
        GridManager.instance.Grid[b.row, b.column] = b.gameObject;
    }


    void SwapMOveCompleted() // 스왑 완료후 체크
    {
        var matched = GridManager.instance.CheckAllBoardMatch();
        if (matched.Count == 0)
        {
            SwapAndCheckMatch(swap_a, swap_b, true); // 복귀
        }
        else
        {
            GridManager.instance.RunCheckAndRemoveAndFill();
        }
    }

}
