using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using System.Threading;
public delegate void NormalCallBack();


public class GridManager : MonoBehaviour
{
    public bool allMoveDone = true;
    public int[] dex = { 1 , -1 };
    public static GridManager instance { get; private set; } // 싱글톤

    public List<Sprite> Sprites = new List<Sprite>();
    public GameObject CandyPrefab;
    public Transform CandyParent;
    public GameObject DoneEffectPref;

    public GameObject GameClear;

    public TextMeshProUGUI text_remainscore;

    public int dimension = 7;
    public float Distance = 1.0f;

    public GameObject[,] Grid;

    private Vector3 posOffset = new Vector3(0.5f, 0.5f, 0);

    public int remainscore = 3;

    public int save_col;
    public int save_row;
    private bool specialcrate = false;
    public bool specialdestroy = false;

    void Awake() { instance = this; }

    void Start()
    {

        Grid = new GameObject[dimension, dimension];

        InitGrid();

        text_remainscore.SetText("3");

    }

    public Vector3 GridIndexToPos(int row, int col) // 그리드에 위치 지정하는 함수
    {
        return new Vector3(col + posOffset.x, row + posOffset.y, 0);
    }


    void InitGrid() // 그리드 초기화
    {
        for (int column = 0; column < dimension; column++)
        {
            for (int row = 0; row < dimension; row++)
            {
                var candy = Instantiate(CandyPrefab, new Vector3(column, row, 0) + posOffset, Quaternion.identity);

                Candy c = candy.GetComponent<Candy>();

                candy.transform.SetParent(CandyParent);
                c.SetRowColumn(row, column);
                Grid[row, column] = candy;
            }
        }
        StartCoroutine(WaitAndCheck());
    }

    IEnumerator WaitAndCheck() // 1초마다 체크
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(CheckAndRemoveAndFill());
    }

    List<GameObject> GetSideMatch(int row, int col, int spriteIndex) // 2X2 블록 체크 함수
    {
        List<GameObject> matched = new List<GameObject>();
        for (int i = 0; i < 2; i++)
        {
            if ((col + dex[i]) > 0 || (col + dex[i] <= dimension))
            {
                if (Grid[row + 1, col + dex[i]].GetComponent<Candy>().spriteIndex == spriteIndex)
                matched.Add(Grid[row + 1, col + dex[i]]);
                break;
            }
        }

        return matched;
    }

    List<GameObject> GetspecialMatch(int row, int col) // 먼치킨 블록을 이동할때
    {
        List<GameObject> matched = new List<GameObject>();
        if(Grid[row, col].GetComponent<Candy>().special)
        {
            switch(Grid[row, col].GetComponent<Candy>().s_direction)
            {
                case 1:
                    Debug.Log("위로 이동.");
                    for (int i = row; i < dimension; i++)
                    {
                        matched.Add(Grid[i, col]);
                    }
                    break;
                case 2:
                    Debug.Log("아래로 이동.");
                    for (int i = row; i >= 0; i--)
                    {
                        matched.Add(Grid[i, col]);
                    }
                    break;
                case 3:
                    Debug.Log("왼쪽으로 이동");
                    for (int i = col; i >= 0; i--)
                    {
                        matched.Add(Grid[row, i]);
                    }
                    break;
                case 4:
                    Debug.Log("오른족으로 이동");
                    for (int i = col; i < dimension; i++)
                    {
                        matched.Add(Grid[row, i]);
                    }
                    break;

                default:
                    break;
            }
        }
        return matched;
    }

    public HashSet<GameObject> CheckAllBoardMatch() // 블록 매칭 체크 함수
    {
        var matched = new HashSet<GameObject>();
        for (int row = 0; row < dimension; row++)
        {
            for (int col = 0; col < dimension; col++)
            {
                var cur = Grid[row, col];
                var spIndex = cur.GetComponent<Candy>().spriteIndex;
                var mat = GetHorizontalMatch(row, col, spIndex);
                var vert = GetVerticalMatch(row, col, spIndex);
                var special = GetspecialMatch(row, col);

                if (special.Count > 0)
                {
                    matched.UnionWith(special);
                    matched.Add(cur);
                }

                else if (mat.Count >= 1 && vert.Count >= 1)
                {
                    var match2x2 = GetSideMatch(row, col, spIndex);
                    if(match2x2.Count > 0)
                    {

                        matched.UnionWith(mat);
                        matched.UnionWith(vert);
                        matched.UnionWith(match2x2);
                        matched.Add(cur);
                        Debug.Log("current : " + row + "," + col);
                        save_col = row;
                        save_row = col;
                        specialcrate = true;
                    }
                }

                else if (mat.Count >= 2)
                {
                    matched.UnionWith(mat);
                    matched.Add(cur);
                }

                else if (vert.Count >= 2)
                {
                    matched.UnionWith(vert);
                    matched.Add(cur);
                }
            }
        }
        return matched;
    }

    // 코루틴을 외부에서 실행하면 object 파괴또는 비활성화시 코루틴도 멈추기때문에 꼭 매니저에서 실행
    public void RunCheckAndRemoveAndFill()
    {
        StartCoroutine(CheckAndRemoveAndFill());
    }

    // 매칭확인 -> 삭제 -> 내리기 -> 매칭확인
    IEnumerator CheckAndRemoveAndFill()
    {
        while (true)
        {
            var matched = CheckAllBoardMatch();
            Debug.Log("matched: " + matched.Count); // 매칭되는 블록이 없으면 빠져나옴
            if (matched.Count == 0)
                break;
            DestroyCandyGO(matched);
            if(!specialdestroy) // 먼치킨 블록을 굴리는 동안 채우지 않음
            {
                FillBlank();
                Checksp();
            }
            yield return new WaitUntil(() => allMoveDone && !specialdestroy);
        }
    }

    public void Checksp() // 먼치킨 블록 생성
    {
        if(specialcrate)
        {
            Grid[save_col, save_row].GetComponent<Candy>().special = true;
            specialcrate = false;
        }
    }
    public void FillBlank() // 블록 드랍 함수
    {
        allMoveDone = false; // 움직임 막음

        for (int col = 0; col < dimension; col++)
        {
            var candies = GetColumnGO(col);
            var newCandy = MakeNewCandy(col, dimension - candies.Count/* - specialcandies.Count*/);
            foreach (var item in newCandy)
                candies.Enqueue(item);

            for (int row = 0; row < dimension; row++)
            {
                var cand = candies.Dequeue();
                var candy = cand.GetComponent<Candy>();

                if (candy.row != row)
                    candy.MoveToBlank(row, col, FillBlankMoveCB);
            }
        }
    }

    void FillBlankMoveCB() // 움직임 체크
    {
        allMoveDone = isAllMoveDone();
    }

    bool isAllMoveDone()
    {
        for (int row = 0; row < dimension; row++)
        {
            for (int col = 0; col < dimension; col++)
            {
                if (Grid[row, col].GetComponent<Candy>().moveDone == false)
                    return false;
            }
        }
        return true;
    }

    Queue<GameObject> MakeNewCandy(int col, int num) // 블록 생성후 부모를 CandyParent로 지정, 큐에 넣고 리턴 -> 비어있는 곳에 캔디만 생성
    {
        Queue<GameObject> res = new Queue<GameObject>();
        for (int i = 0; i < num; i++)
        {
            var candy = Instantiate(CandyPrefab, new Vector3(col, dimension + i, 0) + posOffset, Quaternion.identity);
            candy.transform.SetParent(CandyParent);
            res.Enqueue(candy);
        }
        return res;
    }

    Queue<GameObject> GetColumnGO(int col) // column 정보 받아오기, 현재 column에 블록이 있는지
    {
        Queue<GameObject> objs = new Queue<GameObject>();
        for (int row = 0; row < dimension; row++)
        {
            //var candy = GetComponent<Candy>().special;
            if (Grid[row, col] != null)
                objs.Enqueue(Grid[row, col]);
        }
        return objs;
    }

    public async void DestroyCandyGO(HashSet<GameObject> gos) // 블록을 부수는데 먼치킨 블록이면 비동기로 처리후 채워주기
    {
        if(specialdestroy)
        {
            foreach (var go in gos)
            {
                var eff = Instantiate(DoneEffectPref, go.transform.position, Quaternion.identity);
                eff.GetComponent<doneEffectParent>().sprite = go.GetComponent<SpriteRenderer>().sprite;
                Grid[go.GetComponent<Candy>().row, go.GetComponent<Candy>().column] = null;
                Destroy(go);
                await Task.Delay(500);
            }
            specialdestroy = false;
            FillBlank();
            updateraminscore(-1);
        }
        else
        {
            foreach (var go in gos)
            {
                var eff = Instantiate(DoneEffectPref, go.transform.position, Quaternion.identity);
                eff.GetComponent<doneEffectParent>().sprite = go.GetComponent<SpriteRenderer>().sprite;
                Grid[go.GetComponent<Candy>().row, go.GetComponent<Candy>().column] = null;
                Destroy(go);
            }
        }

    }

    List<GameObject> GetHorizontalMatch(int row, int col, int spriteIndex) // 가로 매칭 확인
    {
        List<GameObject> matched = new List<GameObject>();
        for (int i = col + 1; i < dimension; i++)
        {
            if (Grid[row, i].GetComponent<Candy>().spriteIndex == spriteIndex)
                matched.Add(Grid[row, i]);
            else
                break;
        }
        return matched;
    }

    List<GameObject> GetVerticalMatch(int row, int col, int spriteIndex) // 세로 매칭 확인
    {
        List<GameObject> matched = new List<GameObject>();
        for (int i = row + 1; i < dimension; i++)
        {
            if (Grid[i, col].GetComponent<Candy>().spriteIndex == spriteIndex)
                matched.Add(Grid[i, col]);
            else
                break;
        }
        return matched;
    }

    public void updateraminscore(int score)
    {
        remainscore += score;
        text_remainscore.SetText(remainscore.ToString());
    }

    public void ReStart()
    {
        this.gameObject.SetActive(true);

        text_remainscore.SetText("3");

        remainscore = 3;

        GameClear.SetActive(false);
    }

    private void Update()
    {
        if (remainscore == 0)
        {
            GameClear.SetActive(true);
            StopAllCoroutines();
            this.gameObject.SetActive(false);
        }
    }
}
