using UnityEngine;
using System.Collections.Generic;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ARFurniture
{
    public class ModelCollider : MonoBehaviour
    {
        // 可調整參數
        [Tooltip("體素解析度 - 值越高表示碰撞體越精確，但性能消耗越大")]
        [Range(5, 100)]
        public int voxelResolution = 15;

        [Tooltip("碰撞體膨脹量 - 微小的膨脹可避免因精度問題導致的碰撞間隙")]
        [Range(0, 0.05f)]
        public float inflateAmount = 0f;

        [Tooltip("是否在運行時顯示碰撞體的邊界線框")]
        public bool debugShowColliders = false;

        [Tooltip("是否自動生成碰撞體")]
        public bool autoGenerateColliders = true;

        // 生成的碰撞體容器的引用
        private GameObject colliderContainer;

        // Awake方法 - 組件被初始化時自動調用
        private void Awake()
        {
            if (autoGenerateColliders)
            {
                GenerateColliders();
            }
        }

        // OnValidate方法 - 編輯器中參數被修改時調用
        private void OnValidate()
        {
            // 如果在編輯模式下且參數被修改，重新生成碰撞體
            #if UNITY_EDITOR
            if (!Application.isPlaying && autoGenerateColliders)
            {
                // 避免在預製體階段執行，只在實例化的物件上執行
                if (IsValidSceneObject())
                {
                    // 延遲呼叫以確保所有屬性已正確更新
                    EditorApplication.delayCall += () =>
                    {
                        if (this != null && this.gameObject != null && IsValidSceneObject())
                        {
                            GenerateColliders();
                        }
                    };
                }
            }
            #endif
        }

        #if UNITY_EDITOR
        // 檢查是否為有效的場景物件而非預製體
        private bool IsValidSceneObject()
        {
            return gameObject.scene.name != null && 
                   !string.IsNullOrEmpty(gameObject.scene.path) && 
                   !EditorUtility.IsPersistent(gameObject);
        }
        #endif

        // 從Inspector按鈕調用的方法
        [ContextMenu("生成碰撞體")]
        public void GenerateColliders()
        {
            // 清除現有的碰撞體容器
            if (colliderContainer != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(colliderContainer);
                }
                else
                {
                    DestroyImmediate(colliderContainer);
                }
                colliderContainer = null;
            }

            // 創建新的碰撞體
            colliderContainer = CreateCompositeCollider(gameObject);
        }

        // 創建複合碰撞體的入口方法
        public GameObject CreateCompositeCollider(GameObject modelObject)
        {
            if (modelObject == null)
            {
                Debug.LogError("模型物件為空，無法創建複合碰撞體");
                return null;
            }

            // 移除現有的碰撞體
            foreach (var existingCollider in modelObject.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying)
                {
                    Destroy(existingCollider);
                }
                else
                {
                    DestroyImmediate(existingCollider);
                }
            }

            // 創建一個空物件來放置所有碰撞體
            GameObject colliderContainer = new GameObject("ModelColliders");
            colliderContainer.transform.SetParent(modelObject.transform);
            colliderContainer.transform.localPosition = Vector3.zero;
            colliderContainer.transform.localRotation = Quaternion.identity;
            colliderContainer.transform.localScale = Vector3.one;

            // 計算模型的整體邊界框
            Bounds modelBounds = CalculateModelBounds(modelObject);
            
            // 創建體素網格 - 將模型空間劃分為立方體網格
            float voxelSize = Mathf.Max(modelBounds.size.x, modelBounds.size.y, modelBounds.size.z) / voxelResolution;
            bool[,,] voxelGrid = CreateVoxelGrid(modelObject, modelBounds, voxelSize, voxelResolution);
            
            // 使用體素網格創建方塊碰撞體
            CreateBoxCollidersFromVoxelGrid(voxelGrid, voxelSize, modelBounds, colliderContainer, inflateAmount, debugShowColliders);
            
            Debug.Log($"已創建複合碰撞體，解析度: {voxelResolution}，總共生成碰撞體數量: {colliderContainer.transform.childCount}");

            return colliderContainer;
        }

        // 計算模型的邊界框
        private Bounds CalculateModelBounds(GameObject modelObject)
        {
            Renderer[] renderers = modelObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                // 如果沒有找到 Renderer，返回一個以原點為中心的小邊界框
                return new Bounds(modelObject.transform.position, Vector3.one * 0.1f);
            }

            // 使用第一個 Renderer 的邊界作為初始值
            Bounds bounds = renderers[0].bounds;

            // 合併所有其他 Renderer 的邊界
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        // 創建體素網格來表示模型佔據的空間
        private bool[,,] CreateVoxelGrid(GameObject modelObject, Bounds modelBounds, float voxelSize, int resolution)
        {
            bool[,,] voxelGrid = new bool[resolution, resolution, resolution];
            
            // 獲取所有MeshFilter組件和網格
            MeshFilter[] meshFilters = modelObject.GetComponentsInChildren<MeshFilter>();
            
            // 對每個網格進行體素化
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                
                Mesh mesh = mf.sharedMesh;
                Transform meshTransform = mf.transform;
                
                // 將頂點轉換為模型空間
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                
                // 通過三角形建立體素
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 v1 = meshTransform.TransformPoint(vertices[triangles[i]]);
                    Vector3 v2 = meshTransform.TransformPoint(vertices[triangles[i + 1]]);
                    Vector3 v3 = meshTransform.TransformPoint(vertices[triangles[i + 2]]);
                    
                    // 將三角形光柵化到體素網格中
                    RasterizeTriangle(v1, v2, v3, voxelGrid, modelBounds, voxelSize, resolution);
                }
            }
            
            // 填充網格內部（洪水填充算法）
            FloodFillVoxelGrid(voxelGrid, resolution);
            
            return voxelGrid;
        }

        // 將三角形光柵化到體素網格中
        private void RasterizeTriangle(Vector3 v1, Vector3 v2, Vector3 v3, bool[,,] voxelGrid, Bounds bounds, float voxelSize, int resolution)
        {
            // 計算三角形的AABB（軸對齊邊界框）
            Vector3 min = Vector3.Min(Vector3.Min(v1, v2), v3);
            Vector3 max = Vector3.Max(Vector3.Max(v1, v2), v3);
            
            // 轉換為體素坐標
            Vector3 boundMin = bounds.min;
            
            // 獲取對應的體素網格範圍
            int minX = Mathf.Max(0, Mathf.FloorToInt((min.x - boundMin.x) / voxelSize));
            int minY = Mathf.Max(0, Mathf.FloorToInt((min.y - boundMin.y) / voxelSize));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((min.z - boundMin.z) / voxelSize));
            
            int maxX = Mathf.Min(resolution - 1, Mathf.CeilToInt((max.x - boundMin.x) / voxelSize));
            int maxY = Mathf.Min(resolution - 1, Mathf.CeilToInt((max.y - boundMin.y) / voxelSize));
            int maxZ = Mathf.Min(resolution - 1, Mathf.CeilToInt((max.z - boundMin.z) / voxelSize));
            
            // 簡化的光柵化方法 - 對AABB內的每個體素檢查是否與三角形相交
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        // 轉換回世界坐標的體素中心
                        Vector3 voxelCenter = new Vector3(
                            boundMin.x + (x + 0.5f) * voxelSize,
                            boundMin.y + (y + 0.5f) * voxelSize,
                            boundMin.z + (z + 0.5f) * voxelSize
                        );
                        
                        // 檢查體素與三角形的距離
                        float dist = PointTriangleDistance(voxelCenter, v1, v2, v3);
                        
                        // 如果小於半個體素大小，則標記為實心
                        if (dist <= voxelSize * 0.5f)
                        {
                            voxelGrid[x, y, z] = true;
                        }
                    }
                }
            }
        }

        // 計算點到三角形的距離（簡化版本）
        private float PointTriangleDistance(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // 計算三角形法線
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            
            // 計算平面上的投影點
            float dist = Vector3.Dot(p - a, normal);
            Vector3 projection = p - dist * normal;
            
            // 檢查投影點是否在三角形內
            Vector3 ab = b - a;
            Vector3 bc = c - b;
            Vector3 ca = a - c;
            
            Vector3 ap = projection - a;
            Vector3 bp = projection - b;
            Vector3 cp = projection - c;
            
            // 檢查所有三個交叉積是否指向相同方向
            if (Vector3.Dot(Vector3.Cross(ab, ap), normal) >= 0 &&
                Vector3.Dot(Vector3.Cross(bc, bp), normal) >= 0 &&
                Vector3.Dot(Vector3.Cross(ca, cp), normal) >= 0)
            {
                // 點在三角形內，返回到平面的距離
                return Mathf.Abs(dist);
            }
            
            // 點不在三角形內，計算到最近邊或頂點的距離
            float d1 = PointLineSegmentDistance(p, a, b);
            float d2 = PointLineSegmentDistance(p, b, c);
            float d3 = PointLineSegmentDistance(p, c, a);
            
            return Mathf.Min(d1, Mathf.Min(d2, d3));
        }

        // 計算點到線段的距離
        private float PointLineSegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            Vector3 ap = p - a;
            
            float d = Vector3.Dot(ap, ab);
            float abLenSq = ab.sqrMagnitude;
            
            // 如果投影不在線段上
            if (d <= 0) return ap.magnitude;
            if (d >= abLenSq) return (p - b).magnitude;
            
            // 計算最近點
            Vector3 projection = a + (d / abLenSq) * ab;
            return (p - projection).magnitude;
        }

        // 填充體素網格內部的空洞
        private void FloodFillVoxelGrid(bool[,,] voxelGrid, int resolution)
        {
            // 創建一個訪問標記數組
            bool[,,] visited = new bool[resolution, resolution, resolution];
            
            // 從邊緣開始填充 - 將所有邊界外的空間標記為已訪問
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            
            // 從邊界開始
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        // 只處理邊界體素
                        if (x == 0 || y == 0 || z == 0 || x == resolution - 1 || y == resolution - 1 || z == resolution - 1)
                        {
                            // 如果這個邊界體素是空的，將其加入隊列
                            if (!voxelGrid[x, y, z])
                            {
                                queue.Enqueue(new Vector3Int(x, y, z));
                                visited[x, y, z] = true;
                            }
                        }
                    }
                }
            }
            
            // 向內進行洪水填充 - 標記所有能到達邊界的空體素
            int[] dx = { 1, -1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, 1, -1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, 1, -1 };
            
            while (queue.Count > 0)
            {
                Vector3Int curr = queue.Dequeue();
                
                // 檢查六個相鄰體素
                for (int i = 0; i < 6; i++)
                {
                    int nx = curr.x + dx[i];
                    int ny = curr.y + dy[i];
                    int nz = curr.z + dz[i];
                    
                    // 確保在網格範圍內
                    if (nx >= 0 && ny >= 0 && nz >= 0 && nx < resolution && ny < resolution && nz < resolution)
                    {
                        // 如果相鄰體素是空的且尚未訪問
                        if (!voxelGrid[nx, ny, nz] && !visited[nx, ny, nz])
                        {
                            queue.Enqueue(new Vector3Int(nx, ny, nz));
                            visited[nx, ny, nz] = true;
                        }
                    }
                }
            }
            
            // 所有未訪問的空體素都是內部空洞，應將其填充
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        // 如果是空體素但未在洪水填充時訪問，則它是內部空洞
                        if (!voxelGrid[x, y, z] && !visited[x, y, z])
                        {
                            voxelGrid[x, y, z] = true;  // 填充內部空洞
                        }
                    }
                }
            }
        }

        // 從體素網格創建盒型碰撞體
        private void CreateBoxCollidersFromVoxelGrid(bool[,,] voxelGrid, float voxelSize, Bounds modelBounds, GameObject container, float inflateAmount, bool debugMode = false)
        {
            int width = voxelGrid.GetLength(0);
            int height = voxelGrid.GetLength(1);
            int depth = voxelGrid.GetLength(2);
            
            // 用於合併體素的訪問標記
            bool[,,] processed = new bool[width, height, depth];
            
            Vector3 boundMin = modelBounds.min;
            
            int colliderCount = 0;
            
            // 遍歷所有體素，嘗試創建最大尺寸的盒型碰撞體
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        // 跳過空體素或已處理的體素
                        if (!voxelGrid[x, y, z] || processed[x, y, z]) continue;
                        
                        // 找出可以合併的最大體素塊（貪婪算法）
                        int sizeX = 1, sizeY = 1, sizeZ = 1;
                        
                        // 嘗試擴展X方向
                        while (x + sizeX < width && CanExpandX(voxelGrid, processed, x, y, z, sizeX, sizeY, sizeZ))
                        {
                            sizeX++;
                        }
                        
                        // 嘗試擴展Y方向
                        while (y + sizeY < height && CanExpandY(voxelGrid, processed, x, y, z, sizeX, sizeY, sizeZ))
                        {
                            sizeY++;
                        }
                        
                        // 嘗試擴展Z方向
                        while (z + sizeZ < depth && CanExpandZ(voxelGrid, processed, x, y, z, sizeX, sizeY, sizeZ))
                        {
                            sizeZ++;
                        }
                        
                        // 標記所有已合併的體素為已處理
                        for (int i = 0; i < sizeX; i++)
                        {
                            for (int j = 0; j < sizeY; j++)
                            {
                                for (int k = 0; k < sizeZ; k++)
                                {
                                    processed[x + i, y + j, z + k] = true;
                                }
                            }
                        }
                        
                        // 如果體素塊的體積太小，則忽略它
                        if (sizeX * sizeY * sizeZ < 2) continue;
                        
                        // 創建對應的盒型碰撞體
                        GameObject boxObj = new GameObject($"BoxCollider_{colliderCount++}");
                        boxObj.transform.SetParent(container.transform);
                        
                        // 設置位置為體素塊的中心
                        Vector3 center = new Vector3(
                            boundMin.x + (x + sizeX * 0.5f) * voxelSize,
                            boundMin.y + (y + sizeY * 0.5f) * voxelSize,
                            boundMin.z + (z + sizeZ * 0.5f) * voxelSize
                        );
                        
                        boxObj.transform.position = center;
                        
                        // 創建盒型碰撞體
                        BoxCollider boxCollider = boxObj.AddComponent<BoxCollider>();
                        boxCollider.size = new Vector3(
                            sizeX * voxelSize + inflateAmount,
                            sizeY * voxelSize + inflateAmount,
                            sizeZ * voxelSize + inflateAmount
                        );
                        boxCollider.center = Vector3.zero;  // 相對於物件的本地原點
                        
                        // 如果啟用調試模式，為碰撞體添加可視化組件
                        if (debugMode)
                        {
                            AddDebugVisual(boxObj, boxCollider.size);
                        }
                    }
                }
            }
        }

        // 為碰撞體添加可視化線框（僅供調試）
        private void AddDebugVisual(GameObject boxObj, Vector3 size)
        {
            // 如果已經存在調試線框，就不再添加
            if (boxObj.GetComponent<LineRenderer>() != null) return;
            
            // 創建一個新的物件作為線框容器
            GameObject wireframe = new GameObject("DebugWireframe");
            wireframe.transform.SetParent(boxObj.transform);
            wireframe.transform.localPosition = Vector3.zero;
            wireframe.transform.localRotation = Quaternion.identity;
            
            // 設置線框的頂點
            Vector3 halfSize = size * 0.5f;
            Vector3[] vertices = new Vector3[8];
            
            // 立方體的8個頂點
            vertices[0] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            vertices[1] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            vertices[2] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            vertices[3] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            vertices[4] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            vertices[5] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            vertices[6] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
            vertices[7] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            
            // 創建12條邊的線段
            int[][] edges = new int[][] {
                new int[] {0, 1}, new int[] {1, 2}, new int[] {2, 3}, new int[] {3, 0}, // 底面
                new int[] {4, 5}, new int[] {5, 6}, new int[] {6, 7}, new int[] {7, 4}, // 頂面
                new int[] {0, 4}, new int[] {1, 5}, new int[] {2, 6}, new int[] {3, 7}  // 連接邊
            };
            
            // 為每個邊創建LineRenderer
            for (int i = 0; i < edges.Length; i++)
            {
                GameObject line = new GameObject($"Line_{i}");
                line.transform.SetParent(wireframe.transform);
                
                LineRenderer lr = line.AddComponent<LineRenderer>();
                lr.startWidth = 0.01f;
                lr.endWidth = 0.01f;
                lr.positionCount = 2;
                lr.useWorldSpace = false;
                
                // 設置線段的兩個端點
                lr.SetPosition(0, vertices[edges[i][0]]);
                lr.SetPosition(1, vertices[edges[i][1]]);
                
                // 設置線段顏色
                lr.startColor = Color.green;
                lr.endColor = Color.green;
                
                // 設置材質
                lr.material = new Material(Shader.Find("Sprites/Default"));
            }
        }

        // 檢查是否可以在X方向擴展
        private bool CanExpandX(bool[,,] voxelGrid, bool[,,] processed, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
        {
            if (x + sizeX >= voxelGrid.GetLength(0)) return false;
            
            for (int j = 0; j < sizeY; j++)
            {
                for (int k = 0; k < sizeZ; k++)
                {
                    if (!voxelGrid[x + sizeX, y + j, z + k] || processed[x + sizeX, y + j, z + k])
                        return false;
                }
            }
            return true;
        }

        // 檢查是否可以在Y方向擴展
        private bool CanExpandY(bool[,,] voxelGrid, bool[,,] processed, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
        {
            if (y + sizeY >= voxelGrid.GetLength(1)) return false;
            
            for (int i = 0; i < sizeX; i++)
            {
                for (int k = 0; k < sizeZ; k++)
                {
                    if (!voxelGrid[x + i, y + sizeY, z + k] || processed[x + i, y + sizeY, z + k])
                        return false;
                }
            }
            return true;
        }

        // 檢查是否可以在Z方向擴展
        private bool CanExpandZ(bool[,,] voxelGrid, bool[,,] processed, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
        {
            if (z + sizeZ >= voxelGrid.GetLength(2)) return false;
            
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    if (!voxelGrid[x + i, y + j, z + sizeZ] || processed[x + i, y + j, z + sizeZ])
                        return false;
                }
            }
            return true;
        }
    }

    #if UNITY_EDITOR
    // 自定義編輯器
    [CustomEditor(typeof(ModelCollider))]
    public class ModelColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 繪製默認的Inspector
            DrawDefaultInspector();

            // 獲取目標組件引用
            ModelCollider modelCollider = (ModelCollider)target;

            // 添加一個生成碰撞體的按鈕
            if (GUILayout.Button("生成複合碰撞體", GUILayout.Height(30)))
            {
                modelCollider.GenerateColliders();
            }
        }
    }
    #endif

    // 使用Unity的Vector3Int結構來儲存整數坐標
    [System.Serializable]
    public struct Vector3Int
    {
        public int x;
        public int y;
        public int z;

        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }
    }
} 