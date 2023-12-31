﻿//----------------------------------------------------------------------------------------------------------------------------------------------------------
// Generates puzzle in runtime -  according to settings, create/place puzzle-pieces in the scene
// Just attach it to any gameObject and setup in Inspector - then you can call it CreatePuzzle() function from any script to start the generation.
//
// IMPORTANT: Source Image and sub-element image should be manually set to RGBA32 and Read/Write enabled (isReadable)
//----------------------------------------------------------------------------------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


[AddComponentMenu("Scripts/Jigsaw Puzzle/Runtime Puzzle Generator")]
public class PuzzleGenerator_Runtime: MonoBehaviour
{
    [Header("Generation Settings")]
    public Texture2D subElement;			// Will be used for sub-elements generation
	public Material material;				// Puzzle custom material
    public Material assembledMaterial;      // Material for assembled pieces
    public int cols = 2;					// Puzzle grid columns number
	public int rows = 2;					// Puzzle rows columns number
    public PuzzleAnchor anchoring;          // Where the puzzle object pivot(center) should be

    [RangeAttribute(0.2f, 5.0f)]
    public float imageScale = 1;            // Rescale source image to have bigger/smaller size of pieces textures
    public int elementBaseSize = 200;		// Size of puzzle piece base	
    public int pixelsPerUnit = 100;         // Sprites resolution

    // Shadow settings
    public bool useShadows;
    public Vector3 shadowOffset = new Vector3(0.1f, -0.1f, 1);
    public Color shadowColor = new Color(0, 0, 0, 0.5f);


    [Header("PuzzleController Settings")]
    // Allowed position/rotation offset to consider piece placed to it origin
    public float allowedDistance = 0.75f;
    public float allowedRotation = 10;

    // Should pieces be rotated during decomposition
    public bool randomizeRotation = false;

    [Header("Decomposition areas Settings")]
    // Sides (around puzzle) where pieces should be moved during decomposition
    public bool decomposeToLeft = true;
    public bool decomposeToRight = true;
    public bool decomposeToTop;
    public bool decomposeToBottom;
    [Space]
    // Decomposition area size and offset
    public Vector3 horizontalAreasSize = new Vector3(5, 5, 5);
    public bool autoHorizontalAreaOffset = true;
    public Vector3 horizontalAreaOffset = new Vector3(1, 0, 1);
    [Space]
    public Vector3 verticalAreasSize = new Vector3(5, 5, 5);
    public bool autoVerticalAreaOffset = true;
    public Vector3 verticalAreaOffset = new Vector3(0, 1, 1);


    // Contatins data about whole puzzle
    string path;
    Texture2D image;
    PuzzleElement[] puzzleGrid;
    [HideInInspector]
    public PuzzleController puzzle;

    
    //============================================================================================================================================================
    // Aggregate function, that processes whole generation from the image
    public PuzzleController CreatePuzzleFromImage(Texture2D _image) 
	{
        if (imageScale != 1)
            image = TextureUtility.Scale(_image, Mathf.RoundToInt(_image.width * imageScale), Mathf.RoundToInt(_image.height * imageScale));
        else
            image = _image;

        puzzle = null;

		Random.InitState (System.DateTime.Now.Millisecond);
        

        // Important generation settings
        elementBaseSize = Mathf.Clamp(elementBaseSize, subElement.width * 2, subElement.width * 4);
        pixelsPerUnit = Mathf.Clamp (pixelsPerUnit, 10, 1024);
		cols = Mathf.Clamp (cols, 2, 35);
		rows = Mathf.Clamp (rows, 2, 35);	

		puzzleGrid = new PuzzleElement[cols*rows];                
        try
		{  
			GeneratePuzzlePieces (cols, rows, subElement, elementBaseSize, image);
			puzzle = CreateGameObjects().AddComponent<PuzzleController>();
            puzzle.pieceMaterial_assembled = assembledMaterial;
            puzzle.anchoring = anchoring;
            puzzle.Prepare();    
        }

		catch(System.Exception ex)
			{Debug.LogWarning("<color=red>SOMETHING WENT WRONG!</color> \n" + ex, gameObject);}


        puzzle.allowedDistance = allowedDistance;
        puzzle.allowedRotation = allowedRotation;
        puzzle.randomizeRotation = randomizeRotation;

        puzzle.decomposeToLeft = decomposeToLeft;
        puzzle.decomposeToRight = decomposeToRight;
        puzzle.decomposeToTop = decomposeToTop;
        puzzle.decomposeToBottom = decomposeToBottom;
        puzzle.horizontalAreasSize = horizontalAreasSize;
        puzzle.autoHorizontalAreaOffset = autoHorizontalAreaOffset;
        puzzle.horizontalAreaOffset = horizontalAreaOffset;
        puzzle.verticalAreasSize = verticalAreasSize;
        puzzle.autoVerticalAreaOffset = autoVerticalAreaOffset;
        puzzle.verticalAreaOffset = verticalAreaOffset;


        PlayerPrefs.SetInt(puzzle.name + "_X", cols);
        PlayerPrefs.SetInt(puzzle.name + "_Y", rows);


        return puzzle;
	}

    //-----------------------------------------------------------------------------------------------------
    //Aggregate function, that processes whole generation using external image available online (imagePath should starts from "http://")  or localy - (imagePath should starts from "file://") 
    public void CreateFromExternalImage(string imagePath)
    {
        puzzle = null;
        image = new Texture2D(1, 1);
        path = imagePath;
        StartCoroutine(LoadTextureFromWeb());
    }

    //-------
    IEnumerator LoadTextureFromWeb()
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogWarning("Probably this is wrong external source: " + path);
            Debug.Log("DON'T FORGET: the path should starts from 'http://'(for online image) or from 'file://'(for local)");
            Debug.Log(www.error);
            StopCoroutine(LoadTextureFromWeb());
        }
        else
            {
                image = ((DownloadHandlerTexture)www.downloadHandler).texture;

                if (imageScale != 1)
                    image = TextureUtility.Scale(image, Mathf.RoundToInt(image.width * imageScale), Mathf.RoundToInt(image.height * imageScale));

                CreatePuzzleFromImage(image);
            }
    }
    
    //-----------------------------------------------------------------------------------------------------
    // Generate puzzle-pieces gameObjects and compose them in the scene
    GameObject CreateGameObjects () 
	{
		Vector2 spriteBaseSize = new Vector2(image.width/(float)cols/pixelsPerUnit, image.height/(float)rows/pixelsPerUnit);
		GameObject puzzle = new GameObject();
		GameObject piece;
		GameObject shadow;
		SpriteRenderer spriteRenderer;
		SpriteRenderer shadowRenderer;
        Vector2 anchorOffset = Vector2.zero;
        if (anchoring == PuzzleAnchor.Center)
            anchorOffset = new Vector2(cols * spriteBaseSize.x / 2, rows * spriteBaseSize.y / 2);

        puzzle.name = "Puzzle_" + image.name + "_" + cols.ToString() + "x" + rows.ToString();

		// Go through array and create gameObjects
		for (int y = 0; y < rows; y++) 
			for (int x = 0; x < cols; x++)
			{    
				// Generate sprite
				piece = new GameObject();
				piece.name = "piece_" + x.ToString() + "x" + y.ToString();
                piece.transform.SetParent(puzzle.transform);
                piece.transform.position = new Vector3(x * spriteBaseSize.x - anchorOffset.x, anchorOffset.y - y * spriteBaseSize.y, piece.transform.position.z);

                spriteRenderer = piece.AddComponent<SpriteRenderer>() as SpriteRenderer;
                spriteRenderer.sprite = Sprite.Create(puzzleGrid[y * cols + x].texture, new Rect(0, 0, puzzleGrid[y * cols + x].texture.width, puzzleGrid[y * cols + x].texture.height), puzzleGrid[y * cols + x].pivot, pixelsPerUnit);
               

				// Generate shadow as darkened copy of originalsprite
				if (useShadows)
				{
					shadow = Instantiate(piece);
					shadow.transform.parent = piece.transform;
					shadow.transform.localPosition = shadowOffset;
					shadow.name = piece.name + "_Shadow";

					shadowRenderer = shadow.GetComponent<SpriteRenderer>();
					shadowRenderer.color = shadowColor;
					shadowRenderer.sortingOrder = -1;
				}

				// Assign custom material to puzzle-piece (if neended)
				if (material) 
					spriteRenderer.material = material; 
                
			}
        
        return puzzle;
	}

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Generate puzzle-pieces textures and order them in puzzleGrid
    Vector2 GeneratePuzzlePieces (int _cols, int _rows, Texture2D _subElement, int _elementBaseSize, Texture2D _image)
	{
		int top, left, bottom, right;

		// Calculate piece aspect-ratio accordingly to image size    
		Vector2 elementSizeRatio = new Vector2 (_image.width/(float)_cols/elementBaseSize, _image.height/(float)_rows/_elementBaseSize);

        // Prepare sub-element variants
        Color[] subElementPixels = _subElement.GetPixels();
        Color[] topPixels = subElementPixels;
        Color[] leftPixels = TextureUtility.Rotate90(subElementPixels, _subElement.width, _subElement.height, false);
        

        // Generation													                                          
        for (int y = 0; y < _rows; y++) 
			for (int x = 0; x < _cols; x++) 
			{ 
				// Calculate shape - which type/variant of sub-elements should be  used for top/left/bottom/right parts of piece (accordingly to shapes of surrounding puzzle-pieces) 
				//	(0 - flat, 1-convex, 2-concave)	
				top		= 	y > 0           ?   -puzzleGrid[((y - 1) * _cols + x)].bottom  :  0;
				left	=   x > 0           ?   -puzzleGrid[(y * _cols + x - 1)].right     :  0;
				bottom  = 	y < (_rows-1) 	?   Random.Range(-1, 1)*2+1  :  0;     
				right	= 	x < (_cols-1)   ?   Random.Range(-1, 1)*2+1  :  0;


                // Prepare element mask 
                puzzleGrid[y * _cols + x] = new PuzzleElement(
                                                                top, left, bottom, right,
                                                                _elementBaseSize,
                                                                _subElement,
                                                                topPixels, leftPixels
                                                             );

                // Extract and mask image-piece to be used as puzzle-piece texture
                puzzleGrid[y*_cols + x].texture = ExtractFromImage (_image, puzzleGrid[y*_cols + x], x, y, _elementBaseSize, elementSizeRatio);   

				// Set pivot to Left-Top corner of puzzle-piece base
				puzzleGrid[y*_cols + x].pivot = new Vector2(
																((float)puzzleGrid[y*_cols + x].pixelOffset.x / puzzleGrid[y*_cols + x].texture.width * elementSizeRatio.x), 
																(1.0f - (float)puzzleGrid[y*_cols + x].pixelOffset.y / puzzleGrid[y*_cols + x].texture.height * elementSizeRatio.y)
															);
			}


		return elementSizeRatio; 
	}

	//----------------------------------------------------------------------------------------------------------------------------------------------------------  
	// Extract and mask image-piece to be used as puzzle-piece texture
	Texture2D ExtractFromImage(Texture2D _image, PuzzleElement _puzzlElement, int _x, int _y, int _elementBaseSize, Vector2 _elementSizeRatio)
	{
		// Get proper piece of image 
        Color[] pixels = _image.GetPixels(
				                            (int) ((_x * _elementBaseSize - _puzzlElement.pixelOffset.x) * _elementSizeRatio.x),
                                            (int) (_image.height - (_y+1) * _elementBaseSize * _elementSizeRatio.y - _puzzlElement.pixelOffset.height * _elementSizeRatio.y),
                                            (int) (_puzzlElement.maskWidth * _elementSizeRatio.x),
                                            (int) (_puzzlElement.maskHeight * _elementSizeRatio.y)
			                            );

        Texture2D result = new Texture2D(
                                            (int) (_puzzlElement.maskWidth * _elementSizeRatio.x),
                                            (int) (_puzzlElement.maskHeight * _elementSizeRatio.y)
                                        );

        // Apply mask
        result.wrapMode = TextureWrapMode.Clamp;
        _puzzlElement.ApplyMask(pixels, ref result);
        
        return result;   
	}

    //----------------------------------------------------------------------------------------------------------------------------------------------------------  
    public Texture2D GetSourceImage()
    {
        return image;
    }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------  
}
