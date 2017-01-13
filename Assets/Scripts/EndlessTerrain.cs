using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that generates an endless terrain by keeping track of chunks of generated terrains.
/// </summary>
public class EndlessTerrain : MonoBehaviour {

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	public LODInfo[] detailLevels; 
	public static float maxViewDst;
	public Transform viewer;
	public Material mapMaterial;

	public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisibleInViewDst;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();

		maxViewDst = detailLevels [detailLevels.Length - 1].visibleDstThreshold;
		chunkSize = mapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
	
		UpdateVisibleChunks ();
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks ();
		}
	}

	/// <summary>
	/// Updates the visible chunks.
	/// </summary>
	void UpdateVisibleChunks() {

		// Hide all terrain chunks and clear the list of visible chunks from last update
		for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
			terrainChunksVisibleLastUpdate [i].SetVisible (false);
		}
		terrainChunksVisibleLastUpdate.Clear ();

		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

		// Loop through the visible chunks based on view distance and set their coordinates
		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				// If chunk is allready generated, make it visible by updating it
				if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
					terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
				} else { // or create a new terrain chunk
					terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
				}
			}
		}
	}

	/// <summary>
	/// Terrain chunk class keeps track of the terrain mesh, its position and level of detail.
	/// </summary>
	public class TerrainChunk {

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;
		MeshCollider meshCollider;

		LODInfo[] detailLevels;
		LODMesh[] lodMeshes;
		LODMesh collisionLODMesh;

		MapData mapData;
		bool mapDataReceived;
		int previousLODIndex = -1;

		/// <summary>
		/// Initializes a new instance of the TerrainChunk class.
		/// </summary>
		/// <param name="coord">Coordinate position amongst other terrain chunks.</param>
		/// <param name="size">Size of the chunk in world units.</param>
		/// <param name="detailLevels">List with level of details.</param>
		/// <param name="parent">Parent transform.</param>
		/// <param name="material">Material to be used.</param>
		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
			this.detailLevels = detailLevels;

			position = coord * size;
			bounds = new Bounds(position, Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x, 0, position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();
			meshRenderer.material = material;

			meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
			SetVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++) {
				lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
				if (detailLevels[i].useForCollider) {
					collisionLODMesh = lodMeshes[i];
				}
			}

			// Ask for map data by threading.
			mapGenerator.RequestMapData(position, OnMapDataReceived);
		}
			
		/// <summary>
		/// Raises the map data received event.
		/// </summary>
		/// <param name="mapData">Map data.</param>
		void OnMapDataReceived(MapData mapData) {
			this.mapData = mapData;
			mapDataReceived = true;

			UpdateTerrainChunk ();
		}

		/// <summary>
		/// Updates the terrain chunks visibility and level of detail.
		/// </summary>
		public void UpdateTerrainChunk() {
			if(mapDataReceived) {
				float viewerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

				if (visible) {
					int lodIndex = 0;

					// Find the correct detail level that this chunk should have based on distance to viewer
					for (int i = 0; i < detailLevels.Length - 1; i++) { 
						if (viewerDstFromNearestEdge > detailLevels [i].visibleDstThreshold) {
							lodIndex = i + 1;
						} else {
							break;
						}
					}

					// We need to change the level of detail of the mesh
					if (lodIndex != previousLODIndex) {
						LODMesh lodMesh = lodMeshes [lodIndex];
						if (lodMesh.hasMesh) { // Mesh is recieved
							previousLODIndex = lodIndex;
							meshFilter.mesh = lodMesh.mesh; // Mesh exists, so draw it
						} else if (!lodMesh.hasRequestedMesh) {
							lodMesh.RequestMesh (mapData); // Request mesh
						}
					}

					// If were within range, create collision mesh
					if (lodIndex == 0) {
						if (collisionLODMesh.hasMesh) { // Mesh is recieved
							meshCollider.sharedMesh = collisionLODMesh.mesh;
						} else if (!collisionLODMesh.hasRequestedMesh) {
							collisionLODMesh.RequestMesh (mapData); // Request mesh
						}
					}

					terrainChunksVisibleLastUpdate.Add (this);
				}

				SetVisible (visible);
			}
		}

		/// <summary>
		/// Hides the terrain chunk
		/// </summary>
		/// <param name="visible">Is visible if set to <c>true</c>.</param>
		public void SetVisible(bool visible) {
			meshObject.SetActive (visible);
		}
		/// <summary>
		/// Determines whether this instance is visible.
		/// </summary>
		/// <returns><c>true</c> if this instance is visible; otherwise, <c>false</c>.</returns>
		public bool IsVisible() {
			return meshObject.activeSelf;
		}
	}

	/// <summary>
	/// Level of detail mesh class. Contains the mesh as well as level of detail info.
	/// </summary>
	class LODMesh {
		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int lod;
		System.Action updateCallback;

		/// <summary>
		/// Initializes a new instance of the <see cref="LODMesh"/> class.
		/// </summary>
		/// <param name="lod">Level of detail.</param>
		/// <param name="updateCallback">Update callback method.</param>
		public LODMesh(int lod, System.Action updateCallback) {
			this.lod = lod;
			this.updateCallback = updateCallback;
		}

		// 
		/// <summary>
		/// Raises the mesh data received event.
		/// </summary>
		/// <param name="meshData">Mesh data.</param>
		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh ();
			hasMesh = true;

			updateCallback ();
		}
			
		/// <summary>
		/// Requests the mesh from map generator
		/// </summary>
		/// <param name="mapData">Map data to be used.</param>
		public void RequestMesh(MapData mapData) {
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData (mapData, lod, OnMeshDataReceived);
		}
	}
		
	/// <summary>
	/// Struct for user customizable list of level of details and their respective distance thresholds.
	/// </summary>
	[System.Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDstThreshold;
		public bool useForCollider;
	}
}
