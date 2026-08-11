using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 随从拖拽：把 Follower 实体在棋盘格之间自由拖拽移动，
/// 松手落在合法格子时产生“吸附到格”的动画（飞向目标格 + 缩放回弹）。
/// </summary>
public class FollowerDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    FollowerEntity _entity;
    RectTransform _rect;
    Canvas _canvas;
    CanvasGroup _group;
    CellSlot _originCell;
    Vector2 _dragOffset;
    Vector2 _dragSize;
    Coroutine _snapRoutine;

    void Awake()
    {
        _entity = GetComponent<FollowerEntity>();
        _rect = GetComponent<RectTransform>();
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
    }

    bool CanDrag()
    {
        if (_entity == null || _entity.FollowerId == 0 || _entity.IsEnemy) return false;
        var bc = BattleController.Instance;
        if (bc != null && bc.State != BattleController.BattleState.Idle && bc.State != BattleController.BattleState.Finished)
            return false;
        return true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag()) return;

        Transform parent = transform.parent;
        _originCell = parent != null ? parent.GetComponent<CellSlot>() : null;

        // 若上一次吸附动画尚未结束（实体还挂在 Canvas 下），以实体记录的格子为起点
        if (_originCell == null && _entity != null && BoardController.Instance != null)
        {
            if (_snapRoutine != null) { StopCoroutine(_snapRoutine); _snapRoutine = null; }
            _originCell = BoardController.Instance.GetCell(_entity.GridX, _entity.GridY);
        }

        // 记录原始尺寸，拖拽期间改为固定大小跟随指针
        _dragSize = new Vector2(_rect.rect.width, _rect.rect.height);
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.sizeDelta = _dragSize;

        transform.SetParent(_canvas.transform, true);
        transform.SetAsLastSibling();

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_canvas.transform, eventData.position, _canvas.worldCamera, out local);
        _dragOffset = _rect.anchoredPosition - local;
        _rect.anchoredPosition = local + _dragOffset;

        _group.alpha = 0.8f;
        _group.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_group == null || _group.blocksRaycasts) return;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_canvas.transform, eventData.position, _canvas.worldCamera, out local);
        _rect.anchoredPosition = local + _dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_group == null) return;

        CellSlot target = RaycastCellSlot(eventData);

        if (target != null && BoardController.Instance != null &&
            BoardController.Instance.MoveFollower(_originCell, target))
        {
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            Vector2 startPos = _rect.anchoredPosition;
            if (_snapRoutine != null) StopCoroutine(_snapRoutine);
            _snapRoutine = StartCoroutine(SnapToCellRoutine(target, startPos));
            return;
        }

        // 无效目标：原样退回
        _group.alpha = 1f;
        _group.blocksRaycasts = true;
        if (_originCell != null)
        {
            transform.SetParent(_originCell.transform, false);
            StretchToCell();
            return;
        }
    }

    CellSlot RaycastCellSlot(PointerEventData eventData)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return null;
        var pd = new PointerEventData(es) { position = eventData.position };
        var results = new List<RaycastResult>();
        es.RaycastAll(pd, results);
        foreach (var r in results)
        {
            if (r.gameObject == gameObject || r.gameObject.transform.IsChildOf(transform)) continue;
            CellSlot cell = r.gameObject.GetComponentInParent<CellSlot>();
            if (cell != null && !cell.isEnemy) return cell;
        }
        return null;
    }

    IEnumerator SnapToCellRoutine(CellSlot cell, Vector2 startPos)
    {
        RectTransform canvasRect = (RectTransform)_canvas.transform;
        Vector2 endPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, cell.GetComponent<RectTransform>().position),
            _canvas.worldCamera, out endPos);

        // 1) 快速飞向目标格中心（吸附感）
        float dur = 0.16f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            _rect.anchoredPosition = Vector2.Lerp(startPos, endPos, k);
            yield return null;
        }
        _rect.anchoredPosition = endPos;

        // 2) 落格：铺满格子 + 缩放回弹
        transform.SetParent(cell.transform, false);
        StretchToCell();
        _rect.localScale = new Vector3(0.82f, 0.82f, 1f);
        float s1 = 1.06f, s2 = 1f;
        float d1 = 0.08f, d2 = 0.07f;
        t = 0f;
        while (t < d1)
        {
            t += Time.deltaTime;
            float k = t / d1;
            _rect.localScale = Vector3.Lerp(new Vector3(0.82f, 0.82f, 1f), new Vector3(s1, s1, 1f), k);
            yield return null;
        }
        t = 0f;
        while (t < d2)
        {
            t += Time.deltaTime;
            float k = t / d2;
            _rect.localScale = Vector3.Lerp(new Vector3(s1, s1, 1f), new Vector3(s2, s2, 1f), k);
            yield return null;
        }
        _rect.localScale = Vector3.one;
        _snapRoutine = null;
    }

    void StretchToCell()
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
        _rect.localPosition = Vector3.zero;
        _rect.localScale = Vector3.one;
    }
}
