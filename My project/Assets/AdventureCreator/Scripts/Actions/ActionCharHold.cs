/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2023
 *	
 *	"ActionCharHold.cs"
 * 
 *	This action parents a GameObject to a character's hand.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionCharHold : Action
	{

		public int objectToHoldParameterID = -1;

		public int _charID = 0;
		public int objectToHoldID = 0;

		public GameObject objectToHold;

		public bool isPlayer;
		public int playerID = -1;
		public int playerParameterID = -1;

		public Char _char;
		protected Char runtimeChar;

		public Vector3 localEulerAngles;
		public int localEulerAnglesParameterID = -1;

		protected GameObject loadedObject = null;

		public int droppedObjectParameterID = -1;
		protected ActionParameter droppedObjectParameter;

		public AnimationCurve ikTransitionCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
		protected enum CharHoldMethod { ParentObjectToHand, MoveHandWithIK, DropObject };
		[SerializeField] protected CharHoldMethod charHoldMethod = CharHoldMethod.ParentObjectToHand;
		protected enum IKHoldMethod { SetTarget, Release, ReleaseInstantly };
		[SerializeField] protected IKHoldMethod ikHoldMethod = IKHoldMethod.SetTarget;

		[SerializeField] private bool deleteDroppedObject;

		public Hand hand;

		public override ActionCategory Category { get { return ActionCategory.Character; }}
		public override string Title { get { return "Hold object"; }}
		public override string Description { get { return "Parents a GameObject to a Character's hand Transform, as chosen in the Character's inspector. The local transforms of the GameObject will be cleared. Note that this action only works with 3D characters."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			objectToHold = AssignFile (parameters, objectToHoldParameterID, objectToHoldID, objectToHold);

			if (objectToHold != null && !objectToHold.activeInHierarchy)
			{
				loadedObject = (GameObject) Object.Instantiate (objectToHold);
			}

			if (isPlayer)
			{
				runtimeChar = AssignPlayer (playerID, parameters, playerParameterID);
			}
			else
			{
				runtimeChar = AssignFile<Char> (_charID, _char);
			}

			localEulerAngles = AssignVector3 (parameters, localEulerAnglesParameterID, localEulerAngles);

			droppedObjectParameter = GetParameterWithID (parameters, droppedObjectParameterID);
			if (droppedObjectParameter != null && droppedObjectParameter.parameterType != ParameterType.GameObject)
			{
				droppedObjectParameter = null;
			}
		}


		protected GameObject GetObjectToHold ()
		{
			if (loadedObject)
			{
				return loadedObject;
			}
			return objectToHold;
		}


		public override float Run ()
		{
			return RunSelf (false);
		}


		private float RunSelf (bool isSkipping)
		{
			if (runtimeChar != null)
			{
				if (charHoldMethod == CharHoldMethod.MoveHandWithIK && runtimeChar.GetAnimEngine ().IKEnabled)
				{
					if (GetObjectToHold () == null && ikHoldMethod == IKHoldMethod.SetTarget) return 0f;

					switch (hand)
					{
						case Hand.Left:
							ApplyIK (runtimeChar.LeftHandIKController, isSkipping);
							break;

						case Hand.Right:
							ApplyIK (runtimeChar.RightHandIKController, isSkipping);
							break;

						default:
							break;
					}
				}
				else if (charHoldMethod == CharHoldMethod.ParentObjectToHand)
				{
					if (runtimeChar.HoldObject (GetObjectToHold (), hand))
					{
						GetObjectToHold ().transform.localEulerAngles = localEulerAngles;
					}
				}
				else if (charHoldMethod == CharHoldMethod.DropObject)
				{
					GameObject droppedObject = runtimeChar.ReleaseHeldObject (hand, deleteDroppedObject);
					if (droppedObjectParameter != null)
					{
						droppedObjectParameter.SetValue (droppedObject);
					}
				}
			}
			else
			{
				LogWarning ("No character assigned!");
			}
			
			return 0f;
		}


		public override void Skip ()
		{
			RunSelf (true);
		}


		private void ApplyIK (IKLimbController ikLimbController, bool isSkipping = false)
		{
			switch (ikHoldMethod)
			{
				case IKHoldMethod.SetTarget:
					ikLimbController.AddTarget (GetObjectToHold ().transform, ikTransitionCurve, isSkipping);
					break;

				case IKHoldMethod.Release:
					ikLimbController.Clear (isSkipping);
					break;

				case IKHoldMethod.ReleaseInstantly:
					ikLimbController.Clear (true);
					break;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			Char editorChar = _char;

			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					playerParameterID = ChooseParameterGUI ("Player ID:", parameters, playerParameterID, ParameterType.Integer);
					if (playerParameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}

				if (playerParameterID < 0)
				{
					if (playerID >= 0)
					{
						PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
						if (playerPrefab != null)
						{
							editorChar = (Application.isPlaying) ? playerPrefab.GetSceneInstance () : playerPrefab.EditorPrefab;
						}
					}
					else
					{
						editorChar = (Application.isPlaying) ? KickStarter.player : AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
					}
				}
			}
			else
			{
				editorChar = (Char) EditorGUILayout.ObjectField ("Character:", _char, typeof (Char), true);
					
				_charID = FieldToID <Char> (editorChar, _charID);
				editorChar = IDToField <Char> (editorChar, _charID, true);

				_char = editorChar;
			}

			if (editorChar && editorChar.GetAnimEngine ())
			{
				if (editorChar.GetAnimEngine ().IKEnabled)
				{
					charHoldMethod = (CharHoldMethod) EditorGUILayout.EnumPopup ("Hold method:", charHoldMethod);
					if (charHoldMethod == CharHoldMethod.MoveHandWithIK)
					{
						ikHoldMethod = (IKHoldMethod) EditorGUILayout.EnumPopup ("IK command:", ikHoldMethod);
					}
				}
				else if (charHoldMethod == CharHoldMethod.MoveHandWithIK)
				{
					charHoldMethod = CharHoldMethod.ParentObjectToHand;
				}
			}

			if (charHoldMethod == CharHoldMethod.ParentObjectToHand || (charHoldMethod == CharHoldMethod.MoveHandWithIK && ikHoldMethod == IKHoldMethod.SetTarget))
			{
				objectToHoldParameterID = Action.ChooseParameterGUI ("Object to hold:", parameters, objectToHoldParameterID, ParameterType.GameObject);
				if (objectToHoldParameterID >= 0)
				{
					objectToHoldID = 0;
					objectToHold = null;
				}
				else
				{
					objectToHold = (GameObject)EditorGUILayout.ObjectField ("Object to hold:", objectToHold, typeof (GameObject), true);

					objectToHoldID = FieldToID (objectToHold, objectToHoldID);
					objectToHold = IDToField (objectToHold, objectToHoldID, false);
				}
			}
					
			hand = (Hand) EditorGUILayout.EnumPopup ("Hand:", hand);

			if (charHoldMethod == CharHoldMethod.ParentObjectToHand)
			{
				localEulerAnglesParameterID = Action.ChooseParameterGUI ("Object local angles:", parameters, localEulerAnglesParameterID, ParameterType.Vector3);
				if (localEulerAnglesParameterID < 0)
				{
					localEulerAngles = EditorGUILayout.Vector3Field ("Object local angles:", localEulerAngles);
				}
			}
			else if (charHoldMethod == CharHoldMethod.MoveHandWithIK && ikHoldMethod == IKHoldMethod.SetTarget)
			{
				ikTransitionCurve = EditorGUILayout.CurveField ("Transition curve:", ikTransitionCurve);
			}
			else if (charHoldMethod == CharHoldMethod.DropObject)
			{
				deleteDroppedObject = EditorGUILayout.Toggle ("Delete dropped object?", deleteDroppedObject);

				if (!deleteDroppedObject)
				{
					droppedObjectParameterID = ChooseParameterGUI ("Dropped object assignment:", parameters, droppedObjectParameterID, ParameterType.GameObject);
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				if (!isPlayer && _char != null && !_char.IsPlayer)
				{
					AddSaveScript <RememberNPC> (_char);
				}

				AddSaveScript <RememberTransform> (objectToHold);
				if (objectToHold != null && objectToHold.GetComponent <RememberTransform>())
				{
					objectToHold.GetComponent <RememberTransform>().saveParent = true;
					if (objectToHold.transform.parent)
					{
						AddSaveScript <ConstantID> (objectToHold.transform.parent.gameObject);
					}
				}
			}

			if (!isPlayer)
			{
				_charID = AssignConstantID<Char> (_char, _charID, 0);
			}
			objectToHoldID = AssignConstantID (objectToHold, objectToHoldID, objectToHoldParameterID);
		}

		
		public override string SetLabel ()
		{
			Char editorChar = _char;

			if (isPlayer && playerParameterID < 0)
			{
				if (playerID >= 0)
				{
					PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
					if (playerPrefab != null)
					{
						editorChar = playerPrefab.EditorPrefab;
					}
				}
				else
				{
					editorChar = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
				}
			}

			if (editorChar != null && objectToHold != null)
			{
				return editorChar.name + " hold " + objectToHold.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer)
			{
				if (_char && _char.gameObject == _gameObject) return true;
				if (_charID == id) return true;
			}
			if (isPlayer && _gameObject && _gameObject.GetComponent <Player>() != null) return true;
			if (objectToHoldParameterID < 0)
			{
				if (objectToHold && objectToHold == _gameObject) return true;
				if (objectToHoldID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && playerParameterID < 0) return true;
			return (playerParameterID < 0 && playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Hold object' Action with key variables already set.</summary>
		 * <param name = "characterToUpdate">The character who will hold the object</param>
		 * <param name = "objectToHold">The object that the character is to hold</param>
		 * <param name = "handToUse">Which hand to place the object in (Left, Right)</param>
		 * <param name = "localEulerAngles">The euler angles to apply locally to the object being held</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharHold CreateNew (Char characterToUpdate, GameObject objectToHold, Hand handToUse, Vector3 localEulerAngles = default(Vector3))
		{
			ActionCharHold newAction = CreateNew<ActionCharHold> ();
			newAction._char = characterToUpdate;
			newAction.TryAssignConstantID (newAction._char, ref newAction._charID);
			newAction.objectToHold = objectToHold;
			newAction.TryAssignConstantID (newAction.objectToHold, ref newAction.objectToHoldID);
			newAction.hand = handToUse;
			newAction.localEulerAngles = localEulerAngles;
			return newAction;
		}
		
		
	}

}