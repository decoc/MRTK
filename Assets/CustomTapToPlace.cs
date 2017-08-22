// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;

namespace HoloToolkit.Unity.InputModule
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Interpolator))]
    public class CustomTapToPlace : MonoBehaviour, IInputClickHandler
    {
        public enum PlaceDirection
        {
            LookAtCamera, //カメラを向く
            UprightInTheSurface, //Gaze位置に対して垂直になる
        }

        [Tooltip("設置中のオブジェクトの向きを設定します。デフォルトではカメラの方向を向きます。")]
        public PlaceDirection placeMode = PlaceDirection.LookAtCamera;

        public enum PlaceAlinement
        {
            Upper,
            Middle,
            Bottom
        }

        [Tooltip("設置するオブジェクトの中心位置を設定します。")]
        public PlaceAlinement placeAlinement = PlaceAlinement.Middle;

        [Tooltip("これをtrueに設定すると、空間マッピング以外の場所にもオブジェクトを設置できます。")]
        public bool IsAnyWherePlace = true;

        [Tooltip("設置中のカメラからのオブジェクトの距離")]
        public float DefaultGazeDistance = 2.0f;

        [Tooltip("WorldAnchorとして用いる")]
        public bool UseAsWorldAnchor = false;

        [Tooltip("WorldAnchorStoreのキー名としてアンカーのフレンドリ名を指定する")]
        public string SavedAnchorFriendlyName = "SavedAnchorFriendlyName";

        [Tooltip("現在のオブジェクトの代わりに親オブジェクトを設置する")]
        public bool PlaceParentOnTap;

        [Tooltip("直接の親が望ましくない場合は、タップで移動する親ゲームオブジェクトを指定する")]
        public GameObject ParentGameObjectToPlace;

        [Tooltip("オブジェクトの設置可能回数を1回に限定する")]
        public bool OneShotMode = false;
        private bool IsPlaced = false;

        [Tooltip("これをtrueに設定すると、オブジェクトをタップする必要なく、オブジェクトを移動してシーンに配置することができます。すぐにオブジェクトを配置する場合に便利です。")]
        public bool IsBeingPlaced;

        [Tooltip("これをtrueに設定すると、この動作で空間マッピングのDrawMeshプロパティを制御できます。")]
        public bool AllowMeshVisualizationControl = true;

        /// <summary>
        /// Unity内では、デフォルト設定でレイキャストレイヤーは無視されます。
        /// </summary>
        private const int IgnoreRaycastLayer = 2;

        private Interpolator interpolator;

        private static Dictionary<GameObject, int> defaultLayersCache = new Dictionary<GameObject, int>();

        protected virtual void Start()
        {
            // 必要なコンポーネントがすべてシーンにあるか確認する
            if (WorldAnchorManager.Instance == null)
            {
                Debug.LogError("This script expects that you have a WorldAnchorManager component in your scene.");
            }

            if (WorldAnchorManager.Instance != null)
            {
                // オブジェクトの設置で開始する場合でなければ, World Anchorを置いてください
                if (!IsBeingPlaced)
                {
                    WorldAnchorManager.Instance.AttachAnchor(gameObject, SavedAnchorFriendlyName);
                }
            }

            DetermineParent();

            interpolator = PlaceParentOnTap
                ? ParentGameObjectToPlace.EnsureComponent<Interpolator>()
                : gameObject.EnsureComponent<Interpolator>();

            if (IsBeingPlaced)
            {
                HandlePlacement();
            }
        }

        protected virtual void Update()
        {
            if (!IsBeingPlaced) { return; }

            #region 設置点の算出
            Vector3 headPosition = Camera.main.transform.position;
            Vector3 gazeDirection = Camera.main.transform.forward;

            // SpatialMappingを用いている場合衝突をチェックする。なければGaze位置を用いる。
            RaycastHit hitInfo;

            Vector3 placementPosition;
            Quaternion placementRotation;

            if (SpatialMappingManager.Instance != null &&
                IsAnyWherePlace == false &&
                Physics.Raycast(headPosition, gazeDirection, out hitInfo, 30.0f, SpatialMappingManager.Instance.LayerMask))
            {
                placementPosition = hitInfo.point;
                placementRotation = Quaternion.LookRotation(hitInfo.normal) * Quaternion.Euler(90, 0, 0);
            }
            else
            {
                placementPosition = (GazeManager.Instance.HitObject == null)
                    ? GazeManager.Instance.GazeOrigin + GazeManager.Instance.GazeNormal * DefaultGazeDistance
                    : GazeManager.Instance.HitPosition;

                placementRotation = (GazeManager.Instance.HitObject == null
                    ? Quaternion.identity
                    : Quaternion.LookRotation(GazeManager.Instance.HitNormal) * Quaternion.Euler(90, 0, 0));
            }
            #endregion

            #region 角度調整
            switch (placeMode)
            {
                case PlaceDirection.LookAtCamera:
                    interpolator.SetTargetRotation(Quaternion.Euler(0, Camera.main.transform.localEulerAngles.y, 0));
                    break;
                case PlaceDirection.UprightInTheSurface:
                    interpolator.SetTargetRotation(placementRotation);
                    break;
                default:
                    break;
            }
            #endregion

            #region 位置調整
            //var objectHeight = (PlaceParentOnTap) ? ParentGameObjectToPlace.transform.lossyScale.y / 2f : transform.lossyScale.y / 2f;
            var objectHeight = (PlaceParentOnTap) ? ParentGameObjectToPlace.transform.lossyScale.y / 2f : transform.lossyScale.y / 2f;
            var objectOffset = objectHeight * (placementRotation * Quaternion.Euler(0, -90, 0).eulerAngles.normalized);

            switch (placeAlinement)
            {
                case PlaceAlinement.Upper:
                    placementPosition -= objectOffset;
                    break;
                case PlaceAlinement.Middle:
                    //Do nothing
                    break;
                case PlaceAlinement.Bottom:
                    placementPosition += objectOffset;
                    break;
            }

            if (PlaceParentOnTap)
            {
                placementPosition = ParentGameObjectToPlace.transform.position + (placementPosition - gameObject.transform.position);
            }

            interpolator.SetTargetPosition(placementPosition);
            #endregion
        }

        public virtual void OnInputClicked(InputClickedEventData eventData)
        {
            if (OneShotMode && IsPlaced) { return; }

            // Tap Gestureで、Placing modeのトグルが切り替わる
            IsBeingPlaced = !IsBeingPlaced;
            HandlePlacement();
        }

        private void HandlePlacement()
        {
            if (IsBeingPlaced)
            {
                IsPlaced = false;

                SetLayerRecursively(transform, useDefaultLayer: false);
                InputManager.Instance.AddGlobalListener(gameObject);

                // Placing modeであれば、SpatialMappingのメッシュを表示する
                if (AllowMeshVisualizationControl) { SpatialMappingManager.Instance.DrawVisualMeshes = true; }
#if UNITY_WSA && !UNITY_EDITOR

                // Removes existing world anchor if any exist.
                if(UseAsWorldAnchor) { WorldAnchorManager.Instance.RemoveAnchor(gameObject); }
#endif
            }
            else
            {
                IsPlaced = true;

                SetLayerRecursively(transform, useDefaultLayer: true);
                // タップ中にゲームオブジェクトが追加あるいは削除された場合、キャッシュを削除する
                defaultLayersCache.Clear();
                InputManager.Instance.RemoveGlobalListener(gameObject);

                // Placing モードでなければ、 SpatialMappingのメッシュを隠す
                if (AllowMeshVisualizationControl) { SpatialMappingManager.Instance.DrawVisualMeshes = false; }
#if UNITY_WSA && !UNITY_EDITOR

                // Add world anchor when object placement is done.
                if(UseAsWorldAnchor) { WorldAnchorManager.Instance.AttachAnchor(gameObject, SavedAnchorFriendlyName); }
#endif
            }
        }

        private void DetermineParent()
        {
            if (!PlaceParentOnTap) { return; }

            if (ParentGameObjectToPlace == null)
            {
                if (gameObject.transform.parent == null)
                {
                    Debug.LogWarning("The selected GameObject has no parent.");
                    PlaceParentOnTap = false;
                }
                else
                {
                    Debug.LogWarning("No parent specified. Using immediate parent instead: " + gameObject.transform.parent.gameObject.name);
                    ParentGameObjectToPlace = gameObject.transform.parent.gameObject;
                }
            }

            if (ParentGameObjectToPlace != null && !gameObject.transform.IsChildOf(ParentGameObjectToPlace.transform))
            {
                Debug.LogWarning("The specified parent object is not a parent of this object.");
            }
        }

        private static void SetLayerRecursively(Transform objectToSet, bool useDefaultLayer)
        {
            if (useDefaultLayer)
            {
                int defaultLayerId;
                if (defaultLayersCache.TryGetValue(objectToSet.gameObject, out defaultLayerId))
                {
                    objectToSet.gameObject.layer = defaultLayerId;
                    defaultLayersCache.Remove(objectToSet.gameObject);
                }
            }
            else
            {
                defaultLayersCache.Add(objectToSet.gameObject, objectToSet.gameObject.layer);

                objectToSet.gameObject.layer = IgnoreRaycastLayer;
            }

            for (int i = 0; i < objectToSet.childCount; i++)
            {
                SetLayerRecursively(objectToSet.GetChild(i), useDefaultLayer);
            }
        }
    }
}
