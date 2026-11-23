using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ChillMusicInformationSync.UISync
{
    public static class ControlButtonHider
    {
        private struct OriginalWidthState
        {
            public float OriginalPreferredWidth;
            public float AnimatedWidth;
        }

        private static MusicUI _musicUI;
        private static bool _isButtonsHidden = false;
        private static readonly Dictionary<Transform, OriginalWidthState> _originalWidths = new Dictionary<Transform, OriginalWidthState>();

        public static void GetMusicUIInstance(MusicUI instance)
        {
            if (_musicUI != instance)
            {
                _musicUI = instance;
                _isButtonsHidden = false;
                _originalWidths.Clear();
            }
        }

        public static void HideOriginalButtons()
        {
            if (_musicUI == null || _isButtonsHidden || !_musicUI.gameObject.activeInHierarchy) return;

            Image loopImg = GetField<Image>("_loopChangeButtonImage");
            Image shuffleImg = GetField<Image>("_shuffleChangeButtonImage");
            Transform interfaceTrans = _musicUI.transform.Find("Interface");
            if (interfaceTrans == null) return;

            Transform audioBtn = interfaceTrans.Find("MusicAudioButton");

            _isButtonsHidden = true;

            SafeHide(loopImg?.transform);
            SafeHide(shuffleImg?.transform);
            SafeHide(audioBtn);
        }

        private static void SafeHide(Transform trans)
        {
            if (trans == null) return;

            CanvasGroup cg = trans.gameObject.GetComponent<CanvasGroup>() ?? trans.gameObject.AddComponent<CanvasGroup>();
            LayoutElement le = trans.gameObject.GetComponent<LayoutElement>() ?? trans.gameObject.AddComponent<LayoutElement>();

            if (!_originalWidths.TryGetValue(trans, out OriginalWidthState widthState))
            {
                RectTransform rt = trans as RectTransform;
                float fallbackWidth = rt != null && rt.rect.width > 0f ? rt.rect.width : 40f;
                widthState = new OriginalWidthState
                {
                    OriginalPreferredWidth = le.preferredWidth,
                    AnimatedWidth = le.preferredWidth >= 0f ? le.preferredWidth : fallbackWidth
                };
                _originalWidths[trans] = widthState;
            }

            if (le.preferredWidth < 0f)
            {
                le.preferredWidth = widthState.AnimatedWidth;
            }

            DOTween.Kill(trans);
            DOTween.Kill(cg);
            DOTween.Kill(le);

            cg.blocksRaycasts = false;
            cg.interactable = false;

            DOTween.To(() => trans.localScale, x => trans.localScale = x, Vector3.zero, 0.4f)
                .SetEase(Ease.InBack)
                .SetTarget(trans);

            DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 0.3f)
                .SetTarget(cg);

            DOTween.To(() => le.preferredWidth, x => le.preferredWidth = x, 0f, 0.4f)
                .SetTarget(le);
        }

        public static void RestoreButtons()
        {
            if (_musicUI == null || !_isButtonsHidden) return;

            Transform interfaceTrans = _musicUI.transform.Find("Interface");
            if (interfaceTrans == null)
            {
                _isButtonsHidden = false;
                return;
            }

            bool animate = _musicUI.gameObject.activeInHierarchy;
            SafeRestore(GetField<Image>("_loopChangeButtonImage")?.transform, animate);
            SafeRestore(GetField<Image>("_shuffleChangeButtonImage")?.transform, animate);
            SafeRestore(interfaceTrans.Find("MusicAudioButton"), animate);

            _isButtonsHidden = false;
        }

        private static void SafeRestore(Transform trans, bool animate)
        {
            if (trans == null) return;
            CanvasGroup cg = trans.GetComponent<CanvasGroup>();
            LayoutElement le = trans.GetComponent<LayoutElement>();

            DOTween.Kill(trans);
            if (cg != null) DOTween.Kill(cg);
            if (le != null) DOTween.Kill(le);

            OriginalWidthState widthState = _originalWidths.TryGetValue(trans, out OriginalWidthState saved)
                ? saved
                : new OriginalWidthState { OriginalPreferredWidth = 40f, AnimatedWidth = 40f };

            if (!animate)
            {
                trans.localScale = Vector3.one;
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.blocksRaycasts = true;
                    cg.interactable = true;
                }
                if (le != null)
                {
                    le.preferredWidth = widthState.OriginalPreferredWidth;
                }
                return;
            }

            if (cg != null)
            {
                cg.blocksRaycasts = true;
                cg.interactable = true;
                DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, 0.3f)
                    .SetTarget(cg);
            }

            DOTween.To(() => trans.localScale, x => trans.localScale = x, Vector3.one, 0.5f)
                .SetEase(Ease.OutBack)
                .SetTarget(trans);

            if (le != null)
            {
                DOTween.To(() => le.preferredWidth, x => le.preferredWidth = x, widthState.AnimatedWidth, 0.5f)
                    .SetTarget(le)
                    .OnComplete(() =>
                    {
                        if (le != null)
                        {
                            le.preferredWidth = widthState.OriginalPreferredWidth;
                        }
                    });
            }
        }

        private static T GetField<T>(string fieldName) where T : class
        {
            FieldInfo field = typeof(MusicUI).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(_musicUI) as T;
        }
    }
}