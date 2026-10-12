# SVBRDF Furniture AR APP

以 Unity 開發的 AR 家具擺放 App。從 GitHub 載入 SVBRDF / PBR 材質的家具模型(GLB)，依商品實際尺寸縮放後放到真實地板上，並依相機畫面估測光照，讓虛擬家具貼合現場明暗與色偏。

## Demo

[![PBR AR Demo](https://img.youtube.com/vi/awo08bSYSUc/hqdefault.jpg)](https://www.youtube.com/shorts/awo08bSYSUc)

## 功能

- 從 [PBR_3D](https://github.com/TWAnnoyingKid/PBR_3D) repo 讀取商品清單、圖片與模型，並在本機快取
- 依 `Info.json` 的尺寸(深 x 寬 x 高 公分)自動縮放模型
- AR 平面偵測擺放，可疊放在已放置的模型上
- 編輯模式：移動、旋轉、顯示尺寸框、商品資訊、刪除
- 相機影像光照估測(亮度 / 色偏 → 方向光、環境光、反射、陰影強度)
- 透明陰影接收面與 Lightship 深度遮擋

## 環境

- Unity 2022.3.41f1(URP 14)
- AR Foundation 5.1.5(ARCore / ARKit)、Niantic Lightship ARDK
- glTFast、DOTween
- [Git LFS](https://git-lfs.com/)(`.glb`、`.exr` 以 LFS 管理)

## 使用方式

```bash
git lfs install
git clone https://github.com/blackCat18340/SVBRDF-Furniture-AR-APP-.git
```

1. 用 Unity Hub 開啟專案(版本 2022.3.41f1)
2. 開啟 `Assets/Scenes/StartScene.unity`
3. Build Settings 切到 Android 或 iOS 後 Build 到實機

## 商品資料格式

PBR_3D repo 中每個商品一個資料夾：

```
<商品ID>/
├── Info.json
├── 3d_model/
│   └── *.glb
└── PICS/
    └── *.png / *.jpg / *.webp
```

`Info.json` 範例：

```json
{
  "productName": "SOFA",
  "price": 0,
  "url": "",
  "description": "Local SVBRDF sofa model.",
  "sizeOptions": "90深 x 90寬 x 80高 公分"
}
```
