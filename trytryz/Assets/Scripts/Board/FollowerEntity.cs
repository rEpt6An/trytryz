using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 随从实体：挂在 Follower 预制体上，包含
///   原始属性（随从数据表）+ 永久成长（跨战斗保留）+ 局内成长（战斗结束清除）。
/// 同时负责随从方块的全部战斗表现：
///   立绘、厚血条（当前/最大 + 绿色按百分比填充）、血条上方的攻击倒计时、
///   整块随从方块随 CD 进度从暗到亮、攻击时朝目标方向震动。
/// </summary>
public class FollowerEntity : MonoBehaviour
{
    public int FollowerId { get; private set; }
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public bool IsEnemy { get; private set; }

    /// <summary>随从数据表行（原始属性来源）。</summary>
    public HeroRow Data { get; private set; }

    // ── 原始属性（数据表，永不变化）──
    public int BaseHp;
    public int BaseAtk;
    public int BaseShield;
    public int BaseMagicAtk;
    public int BaseCrit;
    public int BaseHit;
    public int BaseDodge;
    public float BaseCd;

    // ── 永久成长（跨战斗保留，随从此实例独立记录）──
    public int PermHpBonus;
    public int PermAtkBonus;
    public int PermShieldBonus;
    public int PermMagicAtkBonus;
    public int PermCritBonus;
    public int PermHitBonus;
    public int PermDodgeBonus;

    // ── 局内成长（仅本场战斗有效，战斗结束后清零）──
    public int BattleHpBonus;
    public int BattleAtkBonus;
    public int BattleShieldBonus;
    public int BattleMagicAtkBonus;
    public int BattleCritBonus;
    public int BattleHitBonus;
    public int BattleDodgeBonus;

    // ── 当前运行时数值 ──
    public int CurrentHp;
    public int CurrentMaxHp;
    public int CurrentShield;
    public float CdRemaining;
    public float CdTotal;

    public int TotalAtk      { get { return BaseAtk + PermAtkBonus + BattleAtkBonus; } }
    public int TotalMagicAtk { get { return BaseMagicAtk + PermMagicAtkBonus + BattleMagicAtkBonus; } }
    public int TotalCrit     { get { return Mathf.Clamp(BaseCrit + PermCritBonus + BattleCritBonus, 0, 100); } }
    public int TotalHit      { get { return Mathf.Clamp(BaseHit + PermHitBonus + BattleHitBonus, 0, 100); } }
    public int TotalDodge    { get { return Mathf.Clamp(BaseDodge + PermDodgeBonus + BattleDodgeBonus, 0, 100); } }
    public int TotalShield   { get { return BaseShield + PermShieldBonus + BattleShieldBonus; } }
    public bool IsDead       { get { return CurrentHp <= 0; } }

    [Header("Visuals")]
    public Image background;
    public Image photoImage;
    public Slider hpSlider;      // RPG 风格血条：Slider 绿条占比 = 血量百分比
    public Image hpBarFill;      // Slider 的 fillImage（控制颜色）
    public Image cdOverlay;
    public CanvasGroup canvasGroup; // 阵亡时整个随从格子变暗
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI hpBarText;
    public TextMeshProUGUI cdCountdownText;

    /// <summary>是否处于战斗模式（显示战斗实时数据）。</summary>
    public bool InBattle { get; private set; }
    /// <summary>是否需要刷新表现（战斗外只在数据变化时刷新，避免干扰玩家拖拽 Slider）。</summary>
    public bool VisualsDirty { get; private set; } = true;
    int _battleTeam;
    int _battleGx0;
    int _battleGy0;

    Coroutine _shakeRoutine;

    /// <summary>通过随从 id 从数据表初始化该实体。</summary>
    public void Init(int id, int gridX, int gridY, bool isEnemy)
    {
        FollowerId = id;
        GridX = gridX;
        GridY = gridY;
        IsEnemy = isEnemy;

        Data = BoardController.Instance != null ? BoardController.Instance.GetFollowerData(id) : null;
        if (Data != null)
        {
            BaseHp = Data.hp;
            BaseAtk = Data.atk;
            BaseShield = Data.shield;
            BaseMagicAtk = Data.magicAtk;
            BaseCrit = Data.crit;
            BaseHit = Data.hit;
            BaseDodge = Data.dodge;
            BaseCd = Data.cd;
            CdTotal = BaseCd;
        }
        else
        {
            CdTotal = 1f;
        }

        PermHpBonus = PermAtkBonus = PermShieldBonus = PermMagicAtkBonus = 0;
        PermCritBonus = PermHitBonus = PermDodgeBonus = 0;
        BattleHpBonus = BattleAtkBonus = BattleShieldBonus = BattleMagicAtkBonus = 0;
        BattleCritBonus = BattleHitBonus = BattleDodgeBonus = 0;

        CurrentMaxHp = BaseHp;
        CurrentHp = BaseHp;
        CurrentShield = BaseShield;
        CdRemaining = 0f;
        InBattle = false;

        if (background != null)
            background.color = isEnemy ? new Color(0.24f, 0.12f, 0.12f, 0.95f) : new Color(0.12f, 0.14f, 0.2f, 0.95f);

        if (nameText != null) nameText.text = Data != null ? Data.name : "#" + id;
        if (statsText != null)
            statsText.text = Data != null ? "ATK:" + Data.atk + "  CD:" + Data.cd.ToString("F1") + "s" : "";
        LoadPhoto();
        RefreshVisuals();
    }

    void LoadPhoto()
    {
        if (photoImage == null || Data == null || string.IsNullOrEmpty(Data.photo)) return;
        string res = Data.photo;
        const string prefix = "Assets/Resources/";
        if (res.StartsWith(prefix)) res = res.Substring(prefix.Length);
        int dot = res.LastIndexOf('.');
        if (dot > 0) res = res.Substring(0, dot);
        Sprite sp = Resources.Load<Sprite>(res);
        if (sp != null)
        {
            photoImage.sprite = sp;
            photoImage.color = Color.white;
        }
        else
        {
            photoImage.color = new Color(0.32f, 0.32f, 0.38f, 1f);
        }
    }

    public void SetGridPosition(int x, int y)
    {
        GridX = x;
        GridY = y;
    }

    /// <summary>进入战斗模式（gx0/gy0 为 0 基棋盘坐标）。</summary>
    public void EnterBattleMode(int team, int gx0, int gy0)
    {
        InBattle = true;
        _battleTeam = team;
        _battleGx0 = gx0;
        _battleGy0 = gy0;
        if (cdOverlay != null) cdOverlay.gameObject.SetActive(true);
        RefreshVisuals();
    }

    public void ExitBattleMode()
    {
        InBattle = false;
        if (cdOverlay != null) cdOverlay.gameObject.SetActive(false);
        RefreshVisuals();
    }

    /// <summary>战斗结束：局内成长清零，属性回到 原始属性 + 永久成长。</summary>
    public void ResetBattleGrowth()
    {
        BattleHpBonus = 0;
        BattleAtkBonus = 0;
        BattleShieldBonus = 0;
        BattleMagicAtkBonus = 0;
        BattleCritBonus = 0;
        BattleHitBonus = 0;
        BattleDodgeBonus = 0;
        CurrentMaxHp = BaseHp + PermHpBonus;
        CurrentHp = CurrentMaxHp;
        CurrentShield = BaseShield + PermShieldBonus;
        CdRemaining = 0f;
        VisualsDirty = true;
    }

    /// <summary>全新开局：永久成长也清零。</summary>
    public void FullReset()
    {
        ResetBattleGrowth();
        PermHpBonus = 0;
        PermAtkBonus = 0;
        PermShieldBonus = 0;
        PermMagicAtkBonus = 0;
        PermCritBonus = 0;
        PermHitBonus = 0;
        PermDodgeBonus = 0;
        CurrentMaxHp = BaseHp;
        CurrentHp = BaseHp;
        CurrentShield = BaseShield;
        VisualsDirty = true;
    }

    /// <summary>永久成长（例如事件/道具），跨战斗保留。</summary>
    public void AddPermanentGrowth(int hp, int atk, int shield, int magicAtk, int crit, int hit, int dodge)
    {
        PermHpBonus += hp;
        PermAtkBonus += atk;
        PermShieldBonus += shield;
        PermMagicAtkBonus += magicAtk;
        PermCritBonus += crit;
        PermHitBonus += hit;
        PermDodgeBonus += dodge;
        CurrentMaxHp = BaseHp + PermHpBonus + BattleHpBonus;
        CurrentHp = Mathf.Min(CurrentHp + hp, CurrentMaxHp);
        RefreshVisuals();
    }

    /// <summary>局内成长（仅本场战斗有效）。</summary>
    public void AddBattleGrowth(int hp, int atk, int shield, int magicAtk, int crit, int hit, int dodge)
    {
        BattleHpBonus += hp;
        BattleAtkBonus += atk;
        BattleShieldBonus += shield;
        BattleMagicAtkBonus += magicAtk;
        BattleCritBonus += crit;
        BattleHitBonus += hit;
        BattleDodgeBonus += dodge;
        CurrentMaxHp = BaseHp + PermHpBonus + BattleHpBonus;
        CurrentHp = Mathf.Min(CurrentHp + hp, CurrentMaxHp);
        VisualsDirty = true;
    }

    /// <summary>
    /// 刷新随从方块表现：立绘、血条（百分比填充 + 当前/最大）、
    /// 血条上方攻击倒计时、整块随从随 CD 进度从暗到亮。
    /// 战斗中读取 BattleController 实时数据，战斗外使用自身数值。
    /// </summary>
    public void RefreshVisuals()
    {
        VisualsDirty = false;
        bool show = FollowerId != 0;
        if (photoImage != null) photoImage.gameObject.SetActive(show);
        if (nameText != null) nameText.gameObject.SetActive(show);
        if (statsText != null) statsText.gameObject.SetActive(show);
        if (hpBarFill != null) hpBarFill.gameObject.SetActive(show);
        if (hpBarText != null) hpBarText.gameObject.SetActive(show);
        if (cdCountdownText != null) cdCountdownText.gameObject.SetActive(show);
        if (background != null) background.gameObject.SetActive(show);
        if (!show) return;

        int hp = CurrentHp, maxHp = CurrentMaxHp;
        float cdProgress = CdTotal > 0f ? Mathf.Clamp01(1f - CdRemaining / CdTotal) : 1f;
        float remain = Mathf.Max(0f, CdRemaining);

        if (InBattle)
        {
            var bc = BattleController.Instance;
            if (bc != null)
            {
                var bh = bc.GetBattleHeroAt(_battleTeam, _battleGx0, _battleGy0);
                if (bh != null)
                {
                    hp = bh.currentHp;
                    maxHp = bh.maxHp;
                    cdProgress = bh.cd > 0f ? Mathf.Clamp01(1f - bh.cdTimer / bh.cd) : 1f;
                    remain = Mathf.Max(0f, bh.cdTimer);
                }
            }
        }

        // 血条：RPG 风格 Slider 绿条，value = 当前/最大 百分比。
        // 战斗外允许玩家拖拽 Slider；战斗内锁定并跟随实时血量。
        bool dead = hp <= 0;
        float hpPct = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
        if (hpSlider != null)
        {
            hpSlider.interactable = !InBattle;
            hpSlider.SetValueWithoutNotify(hpPct);
        }
        if (hpBarFill != null)
        {
            // 直接驱动填充矩形，保证任何 Image 类型下绿色都按百分比伸缩
            var frt = hpBarFill.rectTransform;
            frt.anchorMin = new Vector2(0f, 0f);
            frt.anchorMax = new Vector2(hpPct, 1f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            hpBarFill.fillAmount = hpPct;
            hpBarFill.color = dead ? new Color(0.22f, 0.4f, 0.22f, 0.95f) : new Color(0.18f, 0.85f, 0.25f, 1f);
        }
        if (hpBarText != null) hpBarText.text = hp + "/" + maxHp;

        // 阵亡：整个随从格子变暗（含立绘/文字/血条）
        if (canvasGroup != null)
            canvasGroup.alpha = dead ? 0.35f : 1f;
        if (background != null)
            background.color = dead ? new Color(0.06f, 0.06f, 0.06f, 0.95f)
                : (IsEnemy ? new Color(0.24f, 0.12f, 0.12f, 0.95f) : new Color(0.12f, 0.14f, 0.2f, 0.95f));
        if (photoImage != null)
        {
            var pc = photoImage.color;
            pc.a = dead ? 0.2f : 1f;
            photoImage.color = pc;
        }

        // 血条上方：下一次攻击剩余时间（战斗中实时，战斗外显示 CD）
        if (cdCountdownText != null)
        {
            cdCountdownText.text = InBattle ? remain.ToString("F1") + "s" : "CD " + CdTotal.ToString("F1") + "s";
        }

        // 整块随从：CD 刚开始全暗 → CD 走完全亮（暗色遮罩从下往上消退）
        if (cdOverlay != null)
        {
            cdOverlay.gameObject.SetActive(InBattle);
            cdOverlay.fillAmount = 1f - cdProgress;
        }
    }

    /// <summary>攻击动效：先小幅后拉蓄力，再朝目标方向突进，最后回弹归位。</summary>
    public void PlayAttackShake(Vector2 towardDir)
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(towardDir));
    }

    System.Collections.IEnumerator ShakeRoutine(Vector2 dir)
    {
        RectTransform rt = (RectTransform)transform;
        Vector3 origin = rt.localPosition;
        Vector2 nd = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;

        const float lungeDist = 26f; // 突进距离（像素），比旧版 9px 明显加大
        const float windupTime = 0.12f;   // 后拉蓄力
        const float strikeTime = 0.18f;   // 向前突进
        const float recoverTime = 0.24f;  // 回弹归位

        // 1) 蓄力：向后小幅后拉
        float t = 0f;
        while (t < windupTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / windupTime);
            rt.localPosition = origin - (Vector3)(nd * lungeDist * 0.35f * k);
            yield return null;
        }

        // 2) 突进：快速冲向目标（easeOut，攻击感）
        t = 0f;
        while (t < strikeTime)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - t / strikeTime, 3f);
            rt.localPosition = origin + (Vector3)(nd * lungeDist * k);
            yield return null;
        }

        // 3) 回弹：轻微过冲后归位
        t = 0f;
        while (t < recoverTime)
        {
            t += Time.deltaTime;
            float k = t / recoverTime;
            float overshoot = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * 0.35f;
            rt.localPosition = origin + (Vector3)(nd * lungeDist * overshoot);
            yield return null;
        }

        rt.localPosition = origin;
    }
}
