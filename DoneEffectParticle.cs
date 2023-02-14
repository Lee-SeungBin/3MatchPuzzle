using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;


public class DoneEffectParticle : MonoBehaviour
{
    public Sprite sprite;

    float fadeSpeed;

    SpriteRenderer spriterenderer;

    void Start()
    {
        spriterenderer = GetComponent<SpriteRenderer>();

        fadeSpeed = transform.parent.GetComponent<doneEffectParent>().fadeSpeed;
        sprite = transform.parent.GetComponent<doneEffectParent>().sprite;
        spriterenderer.sprite = sprite;

        transform.rotation = Quaternion.Euler(0, 0, Random.Range(-60, 60));

        float firstMoveTime = 0.6f;
        //DOTween API 사용
        Sequence s = DOTween.Sequence();
        var pos = transform.position + (Vector3)Random.insideUnitCircle * Random.Range(0.2f, 0.5f);
        s.Append(transform.DOMove(pos, firstMoveTime));
        s.Join(transform.DORotate(new Vector3(0, 0, Random.Range(-15f, 15f)), firstMoveTime))
            .OnComplete(StartFade);

    }


    void StartFade()
    {
        StartCoroutine(DoFade());
    }

    float alpha = 1.0f;
    IEnumerator DoFade()
    {
        while (true)
        {
            alpha -= fadeSpeed;
            spriterenderer.material.color = new Color(spriterenderer.material.color.r,
                spriterenderer.material.color.g, 
                spriterenderer.material.color.b,
                alpha);
            if (alpha <= 0)
                break;
            yield return null;
        }
        Destroy(gameObject);
        if (transform.parent != null)
            Destroy(transform.parent.gameObject);
    }

}
