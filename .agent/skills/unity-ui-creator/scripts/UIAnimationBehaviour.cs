using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIAnimationBehaviour : MonoBehaviour
{
	public enum PanelAnimation
	{
		BottomToTop,
		TopToBottom,
		LeftToRight,
		RightToLeft,
	}

	public AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

	public CanvasGroup AllCanvasGroup;
	public List<CanvasGroup> CanvasGroups = new List<CanvasGroup>();

	[FormerlySerializedAs("Top")]
	public RectTransform UpperAnchor;

	[FormerlySerializedAs("Bottom")]
	public RectTransform LowerAnchor;

	[FormerlySerializedAs("Left")]
	public RectTransform LeftAnchor;

	[FormerlySerializedAs("Right")]
	public RectTransform RightAnchor;

	[Space]
	[FormerlySerializedAs("Panel")]
	public RectTransform PopupAnchor;

	public PanelAnimation PanelShowAnimationType;
	public PanelAnimation PanelHideAnimationType;

	[Header("Color change animation")]
	public Image background;
	public Color ShowColor = new Color(1f, 1f, 1f, 0f);
	public Color HideColor = new Color(1f, 1f, 1f, 0f);

	[Header("Show")]
	public UnityEvent OnAnimationShowStart;
	public UnityEvent OnAnimationShowEnd;

	[Header("Hide")]
	public UnityEvent OnAnimationHideStart;
	public UnityEvent OnAnimationHideEnd;

	public bool IsPanelMovementLeftRight = false;
	public bool IsPanelMovementRightLeft = false;

	public float AnimationTime = 0.5f;
	public bool IsUseMinDeltaForPanel = false;

	public event Action OnHideFromCurrent;

	public static readonly Vector2 UpPosition = new Vector2(0, 2500);
	public static readonly Vector2 DownPosition = new Vector2(0, -2500);
	public static readonly Vector2 LeftPosition = new Vector2(-2500, 0);
	public static readonly Vector2 RightPosition = new Vector2(2500, 0);

	private const float DefaultOffscreenDistance = 2500f;

	private void Awake()
	{
		EnsureInitialized();
	}

	private void OnValidate()
	{
		EnsureCurve();
		if (CanvasGroups == null) CanvasGroups = new List<CanvasGroup>();
	}

	public void Show(Action act = null)
	{
		EnsureInitialized();
		StopAllCoroutines();

		SetShow(false);
		gameObject.SetActive(true);

		SetAllCanvasGroupState(1f, true);

		var isAnimated = false;

		isAnimated |= StartCanvasGroupsFade(0f, 1f, AnimationTime, true);
		isAnimated |= StartBackgroundColorLerp(HideColor, ShowColor, AnimationTime);

		isAnimated |= StartRectMove(UpperAnchor, Vector2.zero, AnimationTime);
		isAnimated |= StartRectMove(LowerAnchor, Vector2.zero, AnimationTime);
		isAnimated |= StartRectMove(LeftAnchor, Vector2.zero, AnimationTime);
		isAnimated |= StartRectMove(RightAnchor, Vector2.zero, AnimationTime);

		isAnimated |= StartRectMove(PopupAnchor, Vector2.zero, AnimationTime);

		OnAnimationShowStart?.Invoke();

		if (isAnimated)
		{
			StartCoroutine(WaitAct(AnimationTime, () =>
			{
				OnAnimationShowEnd?.Invoke();
				act?.Invoke();
			}));
			return;
		}

		OnAnimationShowEnd?.Invoke();
		act?.Invoke();
	}

	public void Hide(Action act = null)
	{
		EnsureInitialized();
		StopAllCoroutines();

		SetShow(true);
		SetAllCanvasGroupState(1f, true);

		var isAnimated = false;

		isAnimated |= StartCanvasGroupsFade(1f, 0f, AnimationTime, false);
		isAnimated |= StartBackgroundColorLerp(ShowColor, HideColor, AnimationTime);

		isAnimated |= StartRectMove(UpperAnchor, UpPosition, AnimationTime);
		isAnimated |= StartRectMove(LowerAnchor, DownPosition, AnimationTime);
		isAnimated |= StartRectMove(LeftAnchor, LeftPosition, AnimationTime);
		isAnimated |= StartRectMove(RightAnchor, RightPosition, AnimationTime);

		if (PopupAnchor)
		{
			isAnimated = true;
			var canvasRect = GetCanvasRectTransform();
			var targetPos = GetOffscreenPosition(PanelHideAnimationType, canvasRect, false);
			StartCoroutine(PanelsAnimation(PopupAnchor, targetPos, AnimationTime));
		}

		OnAnimationHideStart?.Invoke();

		if (isAnimated)
		{
			StartCoroutine(HideAll(AnimationTime, () =>
			{
				OnAnimationHideEnd?.Invoke();
				act?.Invoke();
				gameObject.SetActive(false);
			}));
			return;
		}

		OnAnimationHideEnd?.Invoke();
		act?.Invoke();
		gameObject.SetActive(false);
	}

	public void ShowFromCurrent(Action act = null)
	{
		EnsureInitialized();
		StopAllCoroutines();

		SetAllCanvasGroupState(1f, true);

		StartBackgroundColorLerp(HideColor, ShowColor, AnimationTime);

		StartRectMove(UpperAnchor, Vector2.zero, AnimationTime);
		StartRectMove(LowerAnchor, Vector2.zero, AnimationTime);
		StartRectMove(LeftAnchor, Vector2.zero, AnimationTime);
		StartRectMove(RightAnchor, Vector2.zero, AnimationTime);
		StartRectMove(PopupAnchor, Vector2.zero, AnimationTime);

		StartCoroutine(WaitAct(AnimationTime, act));
	}

	public void HideFromCurrent(Action act = null)
	{
		EnsureInitialized();
		StopAllCoroutines();

		StartCanvasGroupsFade(1f, 0f, AnimationTime, false);
		StartBackgroundColorLerp(ShowColor, HideColor, AnimationTime);

		StartRectMove(UpperAnchor, UpPosition, AnimationTime);
		StartRectMove(LowerAnchor, DownPosition, AnimationTime);
		StartRectMove(LeftAnchor, LeftPosition, AnimationTime);
		StartRectMove(RightAnchor, RightPosition, AnimationTime);

		if (PopupAnchor)
		{
			// English: Legacy behaviour kept for compatibility with existing prefabs.
			var pos = IsPanelMovementLeftRight ? RightPosition : UpPosition;
			if (IsPanelMovementRightLeft) pos = LeftPosition;
			StartRectMove(PopupAnchor, pos, AnimationTime);
		}

		StartCoroutine(HideAll(AnimationTime, act));
	}

	public void SetShow(bool isShowed)
	{
		EnsureInitialized();

		if (AllCanvasGroup)
		{
			AllCanvasGroup.alpha = isShowed ? 1f : 0f;
			AllCanvasGroup.interactable = isShowed;
			AllCanvasGroup.blocksRaycasts = isShowed;
		}

		gameObject.SetActive(isShowed);

		if (CanvasGroups != null)
		{
			for (int i = 0; i < CanvasGroups.Count; i++)
			{
				var group = CanvasGroups[i];
				if (!group) continue;

				group.alpha = isShowed ? 1f : 0f;
				group.interactable = isShowed;
				group.blocksRaycasts = isShowed;
			}
		}

		if (UpperAnchor) UpperAnchor.anchoredPosition = isShowed ? Vector2.zero : UpPosition;
		if (LowerAnchor) LowerAnchor.anchoredPosition = isShowed ? Vector2.zero : DownPosition;
		if (LeftAnchor) LeftAnchor.anchoredPosition = isShowed ? Vector2.zero : LeftPosition;
		if (RightAnchor) RightAnchor.anchoredPosition = isShowed ? Vector2.zero : RightPosition;

		if (PopupAnchor)
		{
			var canvasRect = GetCanvasRectTransform();
			var hiddenPos = GetOffscreenPosition(PanelShowAnimationType, canvasRect, true);
			PopupAnchor.anchoredPosition = isShowed ? Vector2.zero : hiddenPos;
		}
	}

	private void EnsureInitialized()
	{
		EnsureCurve();
		if (CanvasGroups == null) CanvasGroups = new List<CanvasGroup>();
		if (AllCanvasGroup == null) AllCanvasGroup = GetComponent<CanvasGroup>();
	}

	private void EnsureCurve()
	{
		if (curve != null && curve.length > 0) return;

		curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
		curve.preWrapMode = WrapMode.PingPong;
		curve.postWrapMode = WrapMode.PingPong;
	}

	private void SetAllCanvasGroupState(float alpha, bool interactable)
	{
		if (!AllCanvasGroup) return;

		AllCanvasGroup.alpha = alpha;
		AllCanvasGroup.interactable = interactable;
		AllCanvasGroup.blocksRaycasts = interactable;
	}

	private bool StartRectMove(RectTransform rect, Vector2 targetAnchoredPos, float duration)
	{
		if (!rect) return false;
		StartCoroutine(PanelsAnimation(rect, targetAnchoredPos, duration));
		return true;
	}

	private bool StartBackgroundColorLerp(Color from, Color to, float duration)
	{
		if (!background) return false;
		StartCoroutine(ColorAnimation(background, from, to, duration));
		return true;
	}

	private bool StartCanvasGroupsFade(float from, float to, float duration, bool interactableAtEnd)
	{
		if (CanvasGroups == null || CanvasGroups.Count == 0) return false;

		StartCoroutine(CanvasesAnimation(from, to, duration, () =>
		{
			SetCanvasGroupsInteraction(interactableAtEnd);
		}));
		return true;
	}

	private void SetCanvasGroupsInteraction(bool interactable)
	{
		if (CanvasGroups == null) return;

		for (int i = 0; i < CanvasGroups.Count; i++)
		{
			var group = CanvasGroups[i];
			if (!group) continue;

			group.interactable = interactable;
			group.blocksRaycasts = interactable;
		}
	}

	private RectTransform GetCanvasRectTransform()
	{
		var scaler = GetComponentInParent<CanvasScaler>();
		if (scaler) return scaler.GetComponent<RectTransform>();

		var canvas = GetComponentInParent<Canvas>();
		if (canvas) return canvas.GetComponent<RectTransform>();

		return null;
	}

	private Vector2 GetOffscreenPosition(PanelAnimation type, RectTransform canvasRect, bool isForShow)
	{
		var w = DefaultOffscreenDistance;
		var h = DefaultOffscreenDistance;

		if (canvasRect)
		{
			w = Mathf.Max(DefaultOffscreenDistance, canvasRect.rect.width);
			h = Mathf.Max(DefaultOffscreenDistance, canvasRect.rect.height);
		}

		if (type == PanelAnimation.TopToBottom)
		{
			// English: Show starts above, hide ends below.
			return isForShow ? new Vector2(0f, h) : new Vector2(0f, -h);
		}

		if (type == PanelAnimation.BottomToTop)
		{
			return isForShow ? new Vector2(0f, -h) : new Vector2(0f, h);
		}

		if (type == PanelAnimation.LeftToRight)
		{
			return isForShow ? new Vector2(-w, 0f) : new Vector2(w, 0f);
		}

		// type == PanelAnimation.RightToLeft
		return isForShow ? new Vector2(w, 0f) : new Vector2(-w, 0f);
	}

	private IEnumerator HideAll(float animTime, Action act = null)
	{
		yield return new WaitForSecondsRealtime(animTime);

		SetAllCanvasGroupState(0f, false);

		act?.Invoke();
		OnHideFromCurrent?.Invoke();
	}

	private IEnumerator WaitAct(float time, Action act)
	{
		yield return new WaitForSecondsRealtime(time);
		act?.Invoke();
	}

	private IEnumerator PanelsAnimation(RectTransform rect, Vector2 anchoredPosition, float animTime, Action act = null)
	{
		if (!rect)
		{
			act?.Invoke();
			yield break;
		}

		var startPos = rect.anchoredPosition;

		yield return Animate(animTime,
			t => rect.anchoredPosition = Vector2.LerpUnclamped(startPos, anchoredPosition, t),
			() =>
			{
				rect.anchoredPosition = anchoredPosition;
				act?.Invoke();
			});
	}

	private IEnumerator ColorAnimation(Image bg, Color color1, Color color2, float animTime, Action act = null)
	{
		if (!bg)
		{
			act?.Invoke();
			yield break;
		}

		yield return Animate(animTime,
			t => bg.color = Color.LerpUnclamped(color1, color2, t),
			() =>
			{
				bg.color = color2;
				act?.Invoke();
			});
	}

	private IEnumerator CanvasesAnimation(float startAlpha, float endAlpha, float animTime, Action act)
	{
		yield return Animate(animTime,
			t =>
			{
				if (CanvasGroups == null) return;

				for (int i = 0; i < CanvasGroups.Count; i++)
				{
					var group = CanvasGroups[i];
					if (!group) continue;

					group.alpha = Mathf.LerpUnclamped(startAlpha, endAlpha, t);
				}
			},
			() =>
			{
				if (CanvasGroups != null)
				{
					for (int i = 0; i < CanvasGroups.Count; i++)
					{
						var group = CanvasGroups[i];
						if (!group) continue;

						group.alpha = endAlpha;
					}
				}

				act?.Invoke();
			});
	}

	private IEnumerator Animate(float duration, Action<float> onValue, Action onDone)
	{
		EnsureCurve();

		if (duration <= 0f)
		{
			onValue?.Invoke(1f);
			onDone?.Invoke();
			yield break;
		}

		var t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;

			var k = Mathf.Clamp01(t / duration);
			var eased = curve != null ? curve.Evaluate(k) : k;

			onValue?.Invoke(eased);
			yield return null;
		}

		onValue?.Invoke(1f);
		onDone?.Invoke();
	}

	public static IEnumerator FadeAlpha(CanvasGroup group, float targetAlpha, float duration)
	{
		if (group == null) yield break;

		var startAlpha = group.alpha;

		if (duration <= 0f)
		{
			group.alpha = targetAlpha;
			yield break;
		}

		var t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime; // English: Use unscaled time to ignore Time.timeScale
			var k = Mathf.Clamp01(t / duration);

			group.alpha = Mathf.Lerp(startAlpha, targetAlpha, k);
			yield return null;
		}

		group.alpha = targetAlpha;
	}
}
