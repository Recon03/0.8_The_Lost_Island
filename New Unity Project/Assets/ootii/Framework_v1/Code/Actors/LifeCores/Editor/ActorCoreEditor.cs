using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(ActorCore))]
public class ActorCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private ActorCore mTarget;
    private SerializedObject mTargetSO;

    // List object for our Items
    private ReorderableList mEffectList;

    // Store the action types
    private int mEffectIndex = 0;
    private List<Type> mEffectTypes = new List<Type>();
    private List<string> mEffectNames = new List<string>();

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (ActorCore)target;
        mTargetSO = new SerializedObject(target);

        // Update the effects so they can update with the definitions.
        if (!UnityEngine.Application.isPlaying)
        {
            mTarget.InstantiateEffects();
        }

        // Generate the list of motions to display
        Assembly lAssembly = Assembly.GetAssembly(typeof(ActorCoreEffect));
        Type[] lMotionTypes = lAssembly.GetTypes().OrderBy(x => x.Name).ToArray<Type>();
        for (int i = 0; i < lMotionTypes.Length; i++)
        {
            Type lType = lMotionTypes[i];
            if (lType.IsAbstract) { continue; }
            if (typeof(ActorCoreEffect).IsAssignableFrom(lType))
            {
                mEffectTypes.Add(lType);
                mEffectNames.Add(GetFriendlyName(lType));
            }
        }

        // Create the list of items to display
        InstantiateEffectList();
    }

    /// <summary>
    /// This function is called when the scriptable object goes out of scope.
    /// </summary>
    private void OnDisable()
    {
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        if (mEffectList.index >= mTarget._ActiveEffects.Count)
        {
            mEffectList.index = -1;
            mTarget.EditorEffectIndex = -1;
        }

        if (mEffectList.count != mTarget._ActiveEffects.Count)
        {
            InstantiateEffectList();
        }

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Actor Core");

        EditorHelper.DrawInspectorDescription("Very basic foundation for actors. This allows us to set some simple properties.", MessageType.None);

        GUILayout.Space(5);

        GameObject lNewAttributeSourceOwner = EditorHelper.InterfaceOwnerField<IAttributeSource>(new GUIContent("Attribute Source", "Attribute source we'll use to the actor's current health."), mTarget.AttributeSourceOwner, true);
        if (lNewAttributeSourceOwner != mTarget.AttributeSourceOwner)
        {
            mIsDirty = true;
            mTarget.AttributeSourceOwner = lNewAttributeSourceOwner;
        }

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Is Alive", "Determines if the actor is actually alive", mTarget.IsAlive))
        {
            mIsDirty = true;
            mTarget.IsAlive = EditorHelper.FieldBoolValue;
        }

        if (EditorHelper.TextField("Health ID", "Attribute identifier that represents the health attribute", mTarget.HealthID))
        {
            mIsDirty = true;
            mTarget.HealthID = EditorHelper.FieldStringValue;
        }

        GUILayout.Space(5);

        if (EditorHelper.TextField("Damaged Motion", "Name of motion to activate when damage occurs and the message isn't handled.", mTarget.DamagedMotion))
        {
            mIsDirty = true;
            mTarget.DamagedMotion = EditorHelper.FieldStringValue;
        }

        if (EditorHelper.TextField("Death Motion", "Name of motion to activate when death occurs and the message isn't handled.", mTarget.DeathMotion))
        {
            mIsDirty = true;
            mTarget.DeathMotion = EditorHelper.FieldStringValue;
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(5f);

        //// Show the effects
        //EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel, GUILayout.Height(16f));
		//
        //GUILayout.BeginVertical(EditorHelper.GroupBox);
        //EditorHelper.DrawInspectorDescription("Effects that are modifying or controlling the actor.", MessageType.None);
		//
        //mEffectList.DoLayoutList();
		//
        //if (mEffectList.index >= 0)
        //{
            //GUILayout.Space(5f);
            //GUILayout.BeginVertical(EditorHelper.Box);

            //if (mEffectList.index < mTarget._ActiveEffects.Count)
            //{
            //    bool lListIsDirty = DrawEffectDetailItem(mTarget._ActiveEffects[mEffectList.index]);
            //    if (lListIsDirty) { mIsDirty = true; }
            //}
            //else
            //{
            //    mEffectList.index = -1;
            //}
		//
            //GUILayout.EndVertical();
        //}
		//
        //EditorGUILayout.EndVertical();

        // If there is a change... update.
        if (mIsDirty)
        {
            // Flag the object as needing to be saved
            EditorUtility.SetDirty(mTarget);

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            EditorApplication.MarkSceneDirty();
#else
            if (!EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#endif

            // Update all the definitions
            for (int i = 0; i < mTarget._ActiveEffects.Count; i++)
            {
                if (i >= mTarget._EffectDefinitions.Count) { mTarget._EffectDefinitions.Add(""); }
                mTarget._EffectDefinitions[i] = mTarget._ActiveEffects[i].Serialize();
            }

            // Pushes the values back to the runtime so it has the changes
            mTargetSO.ApplyModifiedProperties();

            // Clear out the dirty flag
            mIsDirty = false;
        }
    }

    #region Effects

    /// <summary>
    /// Create the reorderable list
    /// </summary>
    private void InstantiateEffectList()
    {
        mEffectList = new ReorderableList(mTarget._ActiveEffects, typeof(ActorCoreEffect), true, true, true, true);
        mEffectList.drawHeaderCallback = DrawEffectListHeader;
        mEffectList.drawFooterCallback = DrawEffectListFooter;
        mEffectList.drawElementCallback = DrawEffectListItem;
        mEffectList.onAddCallback = OnEffectListItemAdd;
        mEffectList.onRemoveCallback = OnEffectListItemRemove;
        mEffectList.onSelectCallback = OnEffectListItemSelect;
        mEffectList.onReorderCallback = OnEffectListReorder;
        mEffectList.footerHeight = 17f;

        if (mTarget.EditorEffectIndex >= 0 && mTarget.EditorEffectIndex < mEffectList.count)
        {
            mEffectList.index = mTarget.EditorEffectIndex;
        }
    }

    /// <summary>
    /// Header for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawEffectListHeader(Rect rRect)
    {
        EditorGUI.LabelField(rRect, "Spell Effects");

        Rect lNoteRect = new Rect(rRect.width + 12f, rRect.y, 11f, rRect.height);
        EditorGUI.LabelField(lNoteRect, "-", EditorStyles.miniLabel);

        if (GUI.Button(rRect, "", EditorStyles.label))
        {
            mEffectList.index = -1;
            OnEffectListItemSelect(mEffectList);
        }
    }

    /// <summary>
    /// Allows us to draw each item in the list
    /// </summary>
    /// <param name="rRect"></param>
    /// <param name="rIndex"></param>
    /// <param name="rIsActive"></param>
    /// <param name="rIsFocused"></param>
    private void DrawEffectListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
    {
        if (rIndex < mTarget._ActiveEffects.Count)
        {
            ActorCoreEffect lItem = mTarget._ActiveEffects[rIndex];
            if (lItem == null)
            {
                EditorGUI.LabelField(rRect, "NULL");
                return;
            }

            rRect.y += 2;

            string lName = lItem.Name;
            if (lName.Length == 0) { lName = GetFriendlyName(lItem.GetType()); }

            Rect lNameRect = new Rect(rRect.x, rRect.y, rRect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(lNameRect, lName);
        }
    }

    /// <summary>
    /// Footer for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawEffectListFooter(Rect rRect)
    {
        //if (Application.isPlaying)
        {
            Rect lMotionRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
            mEffectIndex = EditorGUI.Popup(lMotionRect, mEffectIndex, mEffectNames.ToArray());
        }

        Rect lAddRect = new Rect(rRect.x + rRect.width - 28 - 28 - 1, rRect.y + 1, 28, 15);
        //if (Application.isPlaying)
        {
            if (GUI.Button(lAddRect, new GUIContent("+", "Add Spell Effect."), EditorStyles.miniButtonLeft)) { OnEffectListItemAdd(mEffectList); }
        }

        Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
        if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Spell Effect."), EditorStyles.miniButtonRight)) { OnEffectListItemRemove(mEffectList); };
    }

    /// <summary>
    /// Allows us to add to a list
    /// </summary>
    /// <param name="rList"></param>
    private void OnEffectListItemAdd(ReorderableList rList)
    {
        if (mEffectIndex >= mEffectTypes.Count) { return; }

        ActorCoreEffect lItem = Activator.CreateInstance(mEffectTypes[mEffectIndex]) as ActorCoreEffect;
        lItem.ActorCore = mTarget;

        mTarget._ActiveEffects.Add(lItem);
        mTarget._EffectDefinitions.Add(lItem.Serialize());

        mEffectList.index = mTarget._ActiveEffects.Count - 1;
        OnEffectListItemSelect(rList);

        mIsDirty = true;
    }

    /// <summary>
    /// Allows us process when a list is selected
    /// </summary>
    /// <param name="rList"></param>
    private void OnEffectListItemSelect(ReorderableList rList)
    {
        mTarget.EditorEffectIndex = rList.index;
    }

    /// <summary>
    /// Allows us to stop before removing the item
    /// </summary>
    /// <param name="rList"></param>
    private void OnEffectListItemRemove(ReorderableList rList)
    {
        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
        {
            int rIndex = rList.index;
            rList.index--;

            mTarget._ActiveEffects.RemoveAt(rIndex);
            mTarget._EffectDefinitions.RemoveAt(rIndex);

            OnEffectListItemSelect(rList);

            mIsDirty = true;
        }
    }

    /// <summary>
    /// Allows us to process after the motions are reordered
    /// </summary>
    /// <param name="rList"></param>
    private void OnEffectListReorder(ReorderableList rList)
    {
        mIsDirty = true;
    }

    /// <summary>
    /// Renders the currently selected step
    /// </summary>
    /// <param name="rStep"></param>
    private bool DrawEffectDetailItem(ActorCoreEffect rItem)
    {
        bool lIsDirty = false;
        if (rItem == null)
        {
            EditorGUILayout.LabelField("NULL");
            return false;
        }

        if (rItem.Name.Length > 0)
        {
            EditorHelper.DrawSmallTitle(rItem.Name.Length > 0 ? rItem.Name : "Actor Core Effect");
        }
        else
        {
            string lName = GetFriendlyName(rItem.GetType());
            EditorHelper.DrawSmallTitle(lName.Length > 0 ? lName : "Actor Core Effect");
        }

        // Render out the Effect specific inspectors
        bool lIsEffectDirty = rItem.OnInspectorGUI(mTarget);
        if (lIsEffectDirty) { lIsDirty = true; }

        if (lIsDirty)
        {
            mTarget._EffectDefinitions[mEffectList.index] = rItem.Serialize();
        }

        return lIsDirty;
    }

#endregion

    /// <summary>
    /// Returns a friendly name for the type
    /// </summary>
    /// <param name="rType"></param>
    /// <returns></returns>
    private string GetFriendlyName(Type rType)
    {
        string lTypeName = rType.Name;
        object[] lMotionAttributes = rType.GetCustomAttributes(typeof(BaseNameAttribute), true);
        if (lMotionAttributes != null && lMotionAttributes.Length > 0) { lTypeName = ((BaseNameAttribute)lMotionAttributes[0]).Value; }

        return lTypeName;
    }
}
