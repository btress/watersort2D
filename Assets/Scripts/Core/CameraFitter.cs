using UnityEngine;

// Tự zoom camera để toàn bộ bàn chơi nằm gọn trong màn hình (kể cả màn 1080x1920 dọc)
public class CameraFitter : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [Tooltip("Khoảng trống quanh bàn chơi, tính theo kích thước 1 ống. Tăng lên = ống nhỏ hơn")]
    [SerializeField] private Vector2 _paddingInBottleSizes = new Vector2(1f, 1f);
    [Tooltip("Camera không zoom gần hơn mức này (tránh ống quá to ở màn ít ống)")]
    [SerializeField] private float _minSize = 3f;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    public void Fit(Bounds board, Vector2 bottleSize)
    {
        float halfWidth = board.extents.x + bottleSize.x * _paddingInBottleSizes.x;
        float halfHeight = board.extents.y + bottleSize.y * _paddingInBottleSizes.y;

        _camera.orthographic = true;
        _camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / _camera.aspect, _minSize);

        Vector3 position = _camera.transform.position;
        _camera.transform.position = new Vector3(board.center.x, board.center.y, position.z);
    }
}
