using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; 
using ChillMusicInformationSync.SMTC;

namespace ChillMusicInformationSync.UISync
{
    public static class MusicCoverMananger
    {
        private static GameObject _trackedCenterIcons;
        private static Vector2 _originalPos = Vector2.zero;
        private static GameObject _currentCover;
        private static bool? _chillPatcherDetected;
        private static Sprite _injectedSprite;
        private static GameObject _savedPlaylistBtn;
        private static Sprite _originalDeactiveSprite;
        private static Sprite _originalActiveSprite;

        private const float AnimDuration = 0.6f;
        private const float CoverSize = 80f;
        private const float Spacing = 20f;

        private static bool IsChillPatcherPresent()
        {
            if (_chillPatcherDetected == null)
                _chillPatcherDetected = Type.GetType(
                    "ChillPatcher.Patches.UIFramework.MusicUI_AlbumArt_Patch, ChillPatcher",
                    false) != null;
            return _chillPatcherDetected.Value;
        }

        private static void InjectCoverToButton(byte[] data)
        {
            var btn = GameObject.Find("IconMusicPlaylist_Button");
            if (btn == null) return;

            var deactive = btn.transform.Find("IconDeactivemage")?.GetComponent<Image>();
            var active = btn.transform.Find("IconActiveImage")?.GetComponent<Image>();
            if (deactive == null || active == null) return;

            if (_savedPlaylistBtn != btn)
            {
                _savedPlaylistBtn = btn;
                _originalDeactiveSprite = deactive.sprite;
                _originalActiveSprite = active.sprite;
            }

            var tex = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(tex, data))
            {
                UnityEngine.Object.Destroy(tex);
                return;
            }

            if (_injectedSprite != null)
            {
                UnityEngine.Object.Destroy(_injectedSprite.texture);
                UnityEngine.Object.Destroy(_injectedSprite);
                _injectedSprite = null;
            }

            _injectedSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            deactive.sprite = _injectedSprite;
            active.sprite = _injectedSprite;
        }

        public static void RefreshCoverInCenterIcons()
        {
            byte[] coverData = SMTCImport.Instance.GetCoverImage();
            if (coverData == null || coverData.Length == 0)
            {
                RemoveCoverFromCenterIcons();
                return;
            }

            // ChillPatcher 共存模式：直接注入封面到按钮图标
            if (IsChillPatcherPresent())
            {
                InjectCoverToButton(coverData);
                return;
            }

            GameObject centerIcons = GameObject.Find("CenterIcons");
            GameObject playlistBtn = GameObject.Find("IconMusicPlaylist_Button");
            if (!centerIcons || !centerIcons.activeInHierarchy || !playlistBtn) return;

            RectTransform centerRect = centerIcons.GetComponent<RectTransform>();
            if (_trackedCenterIcons != centerIcons)
            {
                _trackedCenterIcons = centerIcons;
                _originalPos = centerRect.anchoredPosition;
            }

            Vector2 targetCenterPos = new Vector2(_originalPos.x - (CoverSize + Spacing), _originalPos.y);

            if (_currentCover == null)
            {
                // 1. 创建封面 GameObject
                _currentCover = new GameObject("Mod_SongCover");
                _currentCover.transform.SetParent(centerIcons.transform, false);
                _currentCover.transform.SetAsLastSibling();

                Image img = _currentCover.AddComponent<Image>();
                CanvasGroup cg = _currentCover.AddComponent<CanvasGroup>();
                RectTransform coverRect = _currentCover.GetComponent<RectTransform>();

                coverRect.sizeDelta = new Vector2(CoverSize, CoverSize);
                cg.alpha = 0;
                coverRect.localScale = Vector3.zero;

                DOTween.Kill(centerRect);
                DOTween.To(() => centerRect.anchoredPosition, x => centerRect.anchoredPosition = x, targetCenterPos, AnimDuration)
                    .SetEase(Ease.OutQuint)
                    .SetTarget(centerRect);

                DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, AnimDuration)
                    .SetTarget(cg);

                DOTween.To(() => coverRect.localScale, x => coverRect.localScale = x, Vector3.one, AnimDuration)
                    .SetEase(Ease.OutBack)
                    .SetTarget(coverRect);
            }
            else
            {
                CanvasGroup cg = _currentCover.GetComponent<CanvasGroup>();
                RectTransform coverRect = _currentCover.GetComponent<RectTransform>();

                DOTween.Kill(centerRect);
                DOTween.To(() => centerRect.anchoredPosition, x => centerRect.anchoredPosition = x, targetCenterPos, AnimDuration)
                    .SetEase(Ease.OutQuint)
                    .SetTarget(centerRect);

                if (cg != null)
                {
                    DOTween.Kill(cg);
                    DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, AnimDuration)
                        .SetTarget(cg);
                }
                if (coverRect != null)
                {
                    DOTween.Kill(coverRect);
                    DOTween.To(() => coverRect.localScale, x => coverRect.localScale = x, Vector3.one, AnimDuration)
                        .SetEase(Ease.OutBack)
                        .SetTarget(coverRect);
                }
            }

            UpdateImageSprite(_currentCover.GetComponent<Image>(), coverData);
        }

        public static void RemoveCoverFromCenterIcons()
        {
            // ChillPatcher 共存模式：还原按钮原始图标并清理注入的封面 Sprite
            if (IsChillPatcherPresent())
            {
                if (_savedPlaylistBtn != null)
                {
                    var deactive = _savedPlaylistBtn.transform.Find("IconDeactivemage")?.GetComponent<Image>();
                    var active = _savedPlaylistBtn.transform.Find("IconActiveImage")?.GetComponent<Image>();
                    if (deactive != null) deactive.sprite = _originalDeactiveSprite;
                    if (active != null) active.sprite = _originalActiveSprite;
                }
                if (_injectedSprite != null)
                {
                    UnityEngine.Object.Destroy(_injectedSprite.texture);
                    UnityEngine.Object.Destroy(_injectedSprite);
                    _injectedSprite = null;
                }
                return;
            }

            GameObject centerIcons = GameObject.Find("CenterIcons");
            if (!centerIcons || _currentCover == null) return;

            RectTransform centerRect = centerIcons.GetComponent<RectTransform>();
            GameObject coverToDestroy = _currentCover;
            CanvasGroup cg = coverToDestroy.GetComponent<CanvasGroup>();
            RectTransform coverRect = coverToDestroy.GetComponent<RectTransform>();

            DOTween.Kill(centerRect);
            if (cg != null) DOTween.Kill(cg);
            if (coverRect != null) DOTween.Kill(coverRect);

            // 消失动画
            if (coverRect != null)
            {
                DOTween.To(() => coverRect.localScale, x => coverRect.localScale = x, Vector3.zero, 0.3f)
                    .SetEase(Ease.InBack)
                    .SetTarget(coverRect);
            }
            if (cg != null)
            {
                DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 0.3f)
                    .SetTarget(cg)
                    .OnComplete(() =>
                    {
                        if (coverToDestroy != null)
                        {
                            Image img = coverToDestroy.GetComponent<Image>();
                            if (img != null && img.sprite != null)
                            {
                                UnityEngine.Object.Destroy(img.sprite.texture);
                                UnityEngine.Object.Destroy(img.sprite);
                            }
                            UnityEngine.Object.Destroy(coverToDestroy);
                        }
                        if (_currentCover == coverToDestroy)
                        {
                            _currentCover = null;
                        }
                    });
            }
            else
            {
                UnityEngine.Object.Destroy(coverToDestroy);
                _currentCover = null;
            }

            // 面板弹回
            DOTween.To(() => centerRect.anchoredPosition, x => centerRect.anchoredPosition = x, _originalPos, 0.5f)
                .SetEase(Ease.OutQuad)
                .SetTarget(centerRect);
        }

        private static void UpdateImageSprite(Image img, byte[] data)
        {
            if (img == null) return;
            if (img.sprite != null)
            {
                UnityEngine.Object.Destroy(img.sprite.texture);
                UnityEngine.Object.Destroy(img.sprite);
                img.sprite = null;
            }
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(data))
            {
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                img.preserveAspect = true;
            }
            else
            {
                UnityEngine.Object.Destroy(tex);
            }
        }
    }
}