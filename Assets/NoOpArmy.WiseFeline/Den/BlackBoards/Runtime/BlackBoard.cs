using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NoOpArmy.WiseFeline.BlackBoards
{
    /// <summary>
    /// This component represents a blackboard which allows you to read and write its key value pairs and use them 
    /// in your other component as you see fit.
    /// Let's say you have an enemy agent which wants to use an influence map to search for health packs, the name of the health pack can be set
    /// in a key in the blackboard and then the consideration for searching can read the map name from the blackboard
    /// </summary>
    public class BlackBoard : MonoBehaviour
    {
        /// <summary>
        /// The blackboard definition this component uses
        /// </summary>
        [Tooltip("The blackboard definition this component uses")]
        public BlackBoardDefinition blackBoard;

        /// <summary>
        /// Dictionary of object keys for the blackboard
        /// </summary>
        public Dictionary<string, object> objects = new();

        /// <summary>
        /// Dictionary of UnityEngine.Object keys for the blackboard
        /// </summary>
        public Dictionary<string, UnityEngine.Object> unityObjects = new();

        /// <summary>
        /// Dictionary of Vector3 keys for the blackboard
        /// </summary>
        public Dictionary<string, Vector3> vector3s = new();

        /// <summary>
        /// Dictionary of float keys for the blackboard
        /// </summary>
        public Dictionary<string, float> floats = new();

        /// <summary>
        /// Dictionary of int keys for the blackboard
        /// </summary>
        public Dictionary<string, int> ints = new();

        /// <summary>
        /// Dictionary of bool keys for the blackboard
        /// </summary>
        public Dictionary<string, bool> bools = new();

        /// <summary>
        /// Dictionary of string keys for the blackboard
        /// </summary>
        public Dictionary<string, string> strings = new();

        /// <summary>
        /// Dictionary of GameObject keys for the blackboard
        /// </summary>
        public Dictionary<string, GameObject> gameObjects = new();

        private readonly HashSet<string> keyNames = new();
        private bool isInitialized = false;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (!isInitialized)
            {
                foreach (KeyDefinition d in blackBoard.definitions)
                {
                    if (keyNames.Contains(d.keyName))
                        throw new ArgumentException("Key names should be unique");
                    keyNames.Add(d.keyName);
                    switch (d.keyType)
                    {
                        case KeyDefinition.KeyType.Bool:
                            bools[d.keyName] = false;
                            break;
                        case KeyDefinition.KeyType.String:
                            strings[d.keyName] = "";
                            break;
                        case KeyDefinition.KeyType.Vector3:
                            vector3s[d.keyName] = Vector3.zero;
                            break;
                        case KeyDefinition.KeyType.Object:
                            objects[d.keyName] = objects[d.keyName] = null;
                            break;
                        case KeyDefinition.KeyType.Float:
                            floats[d.keyName] = 0f;
                            break;
                        case KeyDefinition.KeyType.Int:
                            ints[d.keyName] = 0;
                            break;
                        case KeyDefinition.KeyType.UnityObject:
                            unityObjects[d.keyName] = null;
                            break;
                        case KeyDefinition.KeyType.GameObject:
                            gameObjects[d.keyName] = null;
                            break;
                        default:
                            break;
                    }
                }
                isInitialized = true;
            }
        }

        /// <summary>
        /// Sets a Vector3 in the blackboard
        /// </summary>
        /// <param name="key">The key name for the Vector3</param>
        /// <param name="value">The Vector3 to set</param>
        /// <exception cref="System.ArgumentException">Throws if the key is not defined in the blackboard definition as a Vector3</exception>
        public void SetVector3(string key, Vector3 value)
        {
            Init();
            if (vector3s.ContainsKey(key))
                vector3s[key] = value;
            else
                throw new ArgumentException($"The Vector3 key {key} doesn't exist");

        }

        /// <summary>
        /// Returns the Vector3 for the key specified
        /// </summary>
        /// <param name="key">The key to return its value</param>
        /// <returns>The Vector3 associated with the key</returns>
        /// <exception cref="System.ArgumentException">Throws if the key is not defined in the definition as a Vector3</exception>
        public Vector3 GetVector3(string key)
        {
            Init();
            if (vector3s.ContainsKey(key))
                return vector3s[key];
            throw new ArgumentException($"The Vector3 key {key} doesn't exist");
        }

        /// <summary>
        /// Does the key name exist in the vector3 key value pairs?
        /// </summary>
        /// <param name="key">Name of the key to check</param>
        /// <returns>True if the key exists and false otherwise</returns>
        public bool HasVector3(string key)
        {
            Init();
            return vector3s.ContainsKey(key);
        }

        /// <summary>
        /// Sets an object value in the blackboard.
        /// </summary>
        /// <param name="key">The key name for the object value.</param>
        /// <param name="value">The object value to set.</param>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as an object.</exception>
        public void SetObject(string key, object value)
        {
            Init();
            if (objects.ContainsKey(key))
                objects[key] = value;
            else
                throw new ArgumentException($"The object key '{key}' doesn't exist");
        }

        /// <summary>
        /// Retrieves an object value from the blackboard.
        /// </summary>
        /// <param name="key">The key name for the object value.</param>
        /// <returns>The object value associated with the key.</returns>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as an object.</exception>
        public object GetObject(string key)
        {
            Init();
            if (objects.ContainsKey(key))
                return objects[key];
            throw new ArgumentException($"The object key '{key}' doesn't exist");
        }

        /// <summary>
        /// Does the key name exist in the object key value pairs?
        /// </summary>
        /// <param name="key">Name of the key to check</param>
        /// <returns>True if the key exists and false otherwise</returns>
        public bool HasObject(string key)
        {
            Init();
            return objects.ContainsKey(key);
        }


        /// <summary>
        /// Sets a float value in the blackboard.
        /// </summary>
        /// <param name="key">The key name for the float value.</param>
        /// <param name="value">The float value to set.</param>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a float.</exception>
        public void SetFloat(string key, float value)
        {
            Init();
            if (floats.ContainsKey(key))
                floats[key] = value;
            else
                throw new ArgumentException($"The float key '{key}' doesn't exist");
        }

        /// <summary>
        /// Retrieves a float value from the blackboard.
        /// </summary>
        /// <param name="key">The key name for the float value.</param>
        /// <returns>The float value associated with the key.</returns>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a float.</exception>
        public float GetFloat(string key)
        {
            Init();
            if (floats.ContainsKey(key))
                return floats[key];
            throw new ArgumentException($"The float key '{key}' doesn't exist");
        }

        /// <summary>
        /// Does the key name exist in the float key value pairs?
        /// </summary>
        /// <param name="key">Name of the key to check</param>
        /// <returns>True if the key exists and false otherwise</returns>
        public bool HasFloat(string key)
        {
            Init();
            return floats.ContainsKey(key);
        }


        /// <summary>
        /// Sets an integer value in the blackboard.
        /// </summary>
        /// <param name="key">The key name for the integer value.</param>
        /// <param name="value">The integer value to set.</param>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as an integer.</exception>
        public void SetInt(string key, int value)
        {
            Init();
            if (ints.ContainsKey(key))
                ints[key] = value;
            else
                throw new ArgumentException($"The int key '{key}' doesn't exist");
        }

        /// <summary>
        /// Retrieves an integer value from the blackboard.
        /// </summary>
        /// <param name="key">The key name for the integer value.</param>
        /// <returns>The integer value associated with the key.</returns>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as an integer.</exception>
        public int GetInt(string key)
        {
            Init();
            if (ints.ContainsKey(key))
                return ints[key];
            throw new ArgumentException($"The int key '{key}' doesn't exist");
        }

        /// <summary>
        /// Does the key name exist in the int key value pairs?
        /// </summary>
        /// <param name="key">Name of the key to check</param>
        /// <returns>True if the key exists and false otherwise</returns>
        public bool HasInt(string key)
        {
            Init();
            return ints.ContainsKey(key);
        }

        /// <summary>
        /// Sets a boolean value in the blackboard.
        /// </summary>
        /// <param name="key">The key name for the boolean value.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a boolean.</exception>
        public void SetBool(string key, bool value)
        {
            Init();
            if (bools.ContainsKey(key))
                bools[key] = value;
            else
                throw new ArgumentException($"The bool key '{key}' doesn't exist");
        }

        /// <summary>
        /// Sets a string value in the blackboard.
        /// </summary>
        /// <param name="key">The key name for the boolean value.</param>
        /// <param name="value">The boolean value to set.</param>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a boolean.</exception>
        public void SetString(string key, string value)
        {
            Init();
            if (strings.ContainsKey(key))
                strings[key] = value;
            else
                throw new ArgumentException($"The string key '{key}' doesn't exist");
        }

        /// <summary>
        /// Retrieves a boolean value from the blackboard.
        /// </summary>
        /// <param name="key">The key name for the boolean value.</param>
        /// <returns>The boolean value associated with the key.</returns>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a boolean.</exception>
        public bool GetBool(string key)
        {
            Init();
            if (bools.ContainsKey(key))
                return bools[key];
            throw new ArgumentException($"The bool key '{key}' doesn't exist");
        }

        public string GetString(string key)
        {
            Init();
            if (strings.ContainsKey(key))
                return strings[key];
            throw new ArgumentException($"The string key '{key}' doesn't exist");
        }

        /// <summary>
        /// Does the key name exist in the bool key value pairs?
        /// </summary>
        /// <param name="key">Name of the key to check</param>
        /// <returns>True if the key exists and false otherwise</returns>
        public bool HasBool(string key)
        {
            Init();
            return bools.ContainsKey(key);
        }

        /// <summary>
        /// Sets a Unity Object value in the blackboard.
        /// </summary>
        /// <param name="key">The key name for the Unity Object value.</param>
        /// <param name="value">The Unity Object value to set.</param>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a Unity Object.</exception>
        public void SetUnityObject(string key, UnityEngine.Object value)
        {
            Init();
            if (unityObjects.ContainsKey(key))
                unityObjects[key] = value;
            else
                throw new ArgumentException($"The Unity Object key '{key}' doesn't exist");
        }

        /// <summary>
        /// Retrieves a Unity Object value from the blackboard.
        /// </summary>
        /// <param name="key">The key name for the Unity Object value.</param>
        /// <returns>The Unity Object value associated with the key.</returns>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a Unity Object.</exception>
        public UnityEngine.Object GetUnityObject(string key)
        {
            Init();
            if (unityObjects.ContainsKey(key))
                return unityObjects[key];
            throw new ArgumentException($"The Unity Object key '{key}' doesn't exist");
        }

        /// <summary>
        /// Does the key name exist in the UnityObject key value pairs?
        /// </summary>
        /// <param name="key">Name of the key to check</param>
        /// <returns>True if the key exists and false otherwise</returns>
        public bool HasUnityObject(string key)
        {
            Init();
            return unityObjects.ContainsKey(key);
        }


        /// <summary>
        /// Sets a GameObject value in the blackboard.
        /// </summary>
        /// <param name="key">The key name for the GameObject value.</param>
        /// <param name="value">The GameObject value to set.</param>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a GameObject.</exception>
        public void SetGameObject(string key, GameObject value)
        {
            Init();
            if (gameObjects.ContainsKey(key))
                gameObjects[key] = value;
            else
                throw new ArgumentException($"The GameObject key '{key}' doesn't exist");
        }

        /// <summary>
        /// Retrieves a GameObject value from the blackboard.
        /// </summary>
        /// <param name="key">The key name for the GameObject value.</param>
        /// <returns>The GameObject value associated with the key.</returns>
        /// <exception cref="System.ArgumentException">Thrown if the key is not defined in the blackboard definition as a GameObject.</exception>
        public GameObject GetGameObject(string key)
        {
            Init();
            if (gameObjects.ContainsKey(key))
                return gameObjects[key];
            throw new ArgumentException($"The GameObject key '{key}' doesn't exist");
        }

        /// <summary>
        /// Does the key name exist in the GameObject key value pairs?
        /// </summary>
        /// <param name="key">Name of the key to check</param>
        /// <returns>True if the key exists and false otherwise</returns>
        public bool HasGameObject(string key)
        {
            Init();
            return gameObjects.ContainsKey(key);
        }

    }

    /// <summary>
    /// Describes properties of a change to a blackboard when it comes to key name and type
    /// </summary>
    [System.Serializable]
    public class BlackBoardChange
    {
        /// <summary>
        /// The key name and type to change
        /// </summary>
        [Tooltip("The key name and type to change")]
        public KeyDefinition key;

        /// <summary>
        /// If true, adds the value specified to the current value of the key when applicable
        /// Otherwise just set the key to the specified value
        /// </summary>
        [Tooltip("If true, adds the value specified to the current value of the key when applicable. otherwise just set the key to the specified value")]
        public bool addInsteadOfSet = true;

        /// <summary>
        /// Multiply the value to add by delta time before adding
        /// </summary>
        [Tooltip("Multiply the value to add by delta time before adding")]
        public bool multiplyByDeltaTimeForAdds = true;

        /// <summary>
        /// Vector3 value to add/set if the type is Vector3
        /// </summary>
        public Vector3 vectorValue;

        /// <summary>
        /// Int value to add/set if the type is Int
        /// </summary>
        public int intValue;

        /// <summary>
        /// Min Int value to set if the type is Int
        /// </summary>
        public int minIntValue = int.MinValue;

        /// <summary>
        /// Max Int value to set if the type is Int
        /// </summary>
        public int maxIntValue = int.MaxValue;

        /// <summary>
        /// Float value to add/set if the type is Float
        /// </summary>
        public float floatValue;

        /// <summary>
        /// Min float value to set if the type is Float
        /// </summary>
        public float minFloatValue = float.MinValue;

        /// <summary>
        /// max float value to add/set if the type is Float
        /// </summary>
        public float maxFloatValue = float.MaxValue;

        /// <summary>
        /// Bool value to add/set if the type is Bool
        /// </summary>
        public bool boolValue;

        /// <summary>
        /// string value to add/set if the type is Bool
        /// </summary>
        public string stringValue;

        /// <summary>
        /// GameObject value to add/set if the type is GameObject
        /// </summary>
        public GameObject gameObjectValue;

        /// <summary>
        /// UnityEngine.Object value to add/set if the type is UnityObject
        /// </summary>
        public UnityEngine.Object unityObjectValue;

        /// <summary>
        /// Object value to add/set if the type is Object
        /// </summary>
        public object objectValue;

        /// <summary>
        /// Does the change operation based on the value of the other properties of the class
        /// </summary>
        /// <param name="blackBoard">The blackboard to do the changes too</param>
        public void ApplyToBlackboard(BlackBoard blackBoard)
        {

            if (addInsteadOfSet)
            {
                switch (key.keyType)
                {
                    case KeyDefinition.KeyType.Float:
                        var f = blackBoard.GetFloat(key.keyName);
                        var f2 = floatValue;
                        if (multiplyByDeltaTimeForAdds)
                            f2 *= Time.deltaTime;
                        var result = f + f2;
                        result = Mathf.Clamp(result, minFloatValue, maxFloatValue);
                        blackBoard.SetFloat(key.keyName, result);
                        break;
                    case KeyDefinition.KeyType.Vector3:
                        var v = blackBoard.GetVector3(key.keyName);
                        var v2 = vectorValue;
                        if (multiplyByDeltaTimeForAdds)
                            v2 *= Time.deltaTime;
                        blackBoard.SetVector3(key.keyName, v + v2);
                        break;
                    case KeyDefinition.KeyType.Object:
                        blackBoard.SetObject(key.keyName, objectValue);
                        break;
                    case KeyDefinition.KeyType.Int:
                        var i = blackBoard.GetInt(key.keyName);
                        var newInt = i + intValue;
                        newInt = Mathf.Clamp(newInt, minIntValue, maxIntValue);
                        blackBoard.SetInt(key.keyName, newInt);
                        break;
                    case KeyDefinition.KeyType.Bool:
                        blackBoard.SetBool(key.keyName, boolValue);
                        break;
                    case KeyDefinition.KeyType.String:
                        blackBoard.SetString(key.keyName, stringValue);
                        break;
                    case KeyDefinition.KeyType.UnityObject:
                        blackBoard.SetUnityObject(key.keyName, unityObjectValue);
                        break;
                    case KeyDefinition.KeyType.GameObject:
                        blackBoard.SetGameObject(key.keyName, gameObjectValue);
                        break;
                }
            }
            else
            {
                SetOnBlackboard(blackBoard);
            }
        }

        /// <summary>
        /// Writes the value of this change to the key, replacing whatever the key held before.
        /// Unlike <see cref="ApplyToBlackboard"/> this ignores <see cref="addInsteadOfSet"/>, <see cref="multiplyByDeltaTimeForAdds"/>
        /// and the min and max values, so it is the right call for starting values and any other one-off assignment.
        /// </summary>
        /// <param name="blackBoard">The blackboard to write the value to</param>
        public void SetOnBlackboard(BlackBoard blackBoard)
        {
            switch (key.keyType)
            {
                case KeyDefinition.KeyType.Float:
                    blackBoard.SetFloat(key.keyName, floatValue);
                    break;
                case KeyDefinition.KeyType.Vector3:
                    blackBoard.SetVector3(key.keyName, vectorValue);
                    break;
                case KeyDefinition.KeyType.Object:
                    blackBoard.SetObject(key.keyName, objectValue);
                    break;
                case KeyDefinition.KeyType.Int:
                    blackBoard.SetInt(key.keyName, intValue);
                    break;
                case KeyDefinition.KeyType.Bool:
                    blackBoard.SetBool(key.keyName, boolValue);
                    break;
                case KeyDefinition.KeyType.String:
                    blackBoard.SetString(key.keyName, stringValue);
                    break;
                case KeyDefinition.KeyType.UnityObject:
                    blackBoard.SetUnityObject(key.keyName, unityObjectValue);
                    break;
                case KeyDefinition.KeyType.GameObject:
                    blackBoard.SetGameObject(key.keyName, gameObjectValue);
                    break;
            }
        }
    }

    /// <summary>
    /// Put this on a field of type <see cref="BlackBoardChange"/>, or a list or array of them, when the change is always
    /// applied with <see cref="BlackBoardChange.SetOnBlackboard"/>. The inspector then hides the add, deltaTime and min and max
    /// fields, which would otherwise be shown but have no effect.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class BlackBoardSetOnlyAttribute : Attribute
    {
    }

    /// <summary>
    /// Specifieds the properties of a change to a blackboard when it comes to number of times to do the change and delay between changes and ...
    /// </summary>
    [System.Serializable]
    public class BlackBoardChangeer
    {
        /// <summary>
        /// The type of change to happen
        /// </summary>
        public enum ChangeType : byte
        {
            /// <summary>
            /// No change
            /// </summary>
            None,
            /// <summary>
            /// Only once change the blackBoard
            /// </summary>
            Once,
            /// <summary>
            /// Repeat the change for a specified number of times and with a specific delay
            /// </summary>
            RepeatWithDelay,
            /// <summary>
            /// Do a change every frame and multiply the value change by delta time in the case of additive changes
            /// </summary>
            EveryFrame,
        }

        /// <summary>
        /// The type of change which you want to happen to the blackboard
        /// </summary>
        [Tooltip("The type of change which you want to happen to the blackboard")]
        public ChangeType changeType;

        /// <summary>
        /// The delay between changes
        /// </summary>
        [Tooltip("The delay between changes in seconds")]
        public float delayInSeconds;

        /// <summary>
        /// The key name and type hwich you want the change to happen to
        /// </summary>
        [Tooltip("The key name and type which you want the change to happen to")]
        public BlackBoardChange change;

        /// <summary>
        /// Maximum number of times this gets executed in the repeat with delay mode. 0 means unlimited
        /// </summary>
        [Tooltip("Maximum number of times this gets executed in the repeat with delay mode. 0 means unlimited")]
        public int maxExecutionCount = 0;

        private int executionCount;
        private float lastExecution;

        public void Reset()
        {
            executionCount = 0;
        }

        public void Update(BlackBoard blackBoard)
        {

            switch (changeType)
            {
                case ChangeType.None:
                    break;
                case ChangeType.Once:
                    if (executionCount == 0)
                    {
                        executionCount++;
                        lastExecution = Time.time;
                        change.ApplyToBlackboard(blackBoard);
                    }
                    break;
                case ChangeType.RepeatWithDelay:
                    if (Time.time - lastExecution >= delayInSeconds)
                    {
                        if (maxExecutionCount <= 0 || executionCount < maxExecutionCount)
                        {
                            executionCount++;
                            change.ApplyToBlackboard(blackBoard);
                            lastExecution = Time.time;
                        }
                    }
                    break;
                case ChangeType.EveryFrame:
                    executionCount++;
                    change.ApplyToBlackboard(blackBoard);
                    lastExecution = Time.time;
                    break;
                default:
                    break;

            }
        }
    }
}