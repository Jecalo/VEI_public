//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.3.0
//     from Assets/InputSystem/DefaultControl.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @DefaultControl : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @DefaultControl()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""DefaultControl"",
    ""maps"": [
        {
            ""name"": ""Default"",
            ""id"": ""ec293abd-1ab1-4ec6-b5ac-e41267ce9c5d"",
            ""actions"": [
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""13da64d1-5b5a-413b-bfc7-da58dd7d71e2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Interact"",
                    ""type"": ""Button"",
                    ""id"": ""2fcacecb-adbe-4bb4-8176-6159da3291f8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Movement3D"",
                    ""type"": ""Value"",
                    ""id"": ""b900a13b-184b-411b-93e4-444627340d97"",
                    ""expectedControlType"": ""Vector3"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Run"",
                    ""type"": ""Button"",
                    ""id"": ""e677be8b-71da-4ab2-8020-5e0570d080d2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Aim"",
                    ""type"": ""Value"",
                    ""id"": ""3ba700e1-7aef-46eb-b832-fb8bba8e1863"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Menu"",
                    ""type"": ""Button"",
                    ""id"": ""32566117-a075-4a8b-b780-91730cecce31"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Crouch"",
                    ""type"": ""Button"",
                    ""id"": ""862200f8-0400-4dad-b327-0d1ee8e2e195"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f1"",
                    ""type"": ""Button"",
                    ""id"": ""f6c84d59-5d7b-4453-8efd-25f0177ee3de"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f2"",
                    ""type"": ""Button"",
                    ""id"": ""dd92d68e-593c-426e-9de5-409db44848bc"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f3"",
                    ""type"": ""Button"",
                    ""id"": ""26f78c9b-d3bc-478d-a937-df246178c347"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f4"",
                    ""type"": ""Button"",
                    ""id"": ""930a6558-1f44-4c78-8e43-878731e7d801"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f5"",
                    ""type"": ""Button"",
                    ""id"": ""a117c1eb-ff5d-4e9a-918a-25eff9bda426"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f6"",
                    ""type"": ""Button"",
                    ""id"": ""faa945a6-3ead-4d7c-a751-0bdeaf2c8b1e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f7"",
                    ""type"": ""Button"",
                    ""id"": ""ba8eb777-90e0-4755-9c80-5e15e216033a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f8"",
                    ""type"": ""Button"",
                    ""id"": ""51730d81-4eb0-4af6-a9c0-b74fb215b81f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f9"",
                    ""type"": ""Button"",
                    ""id"": ""13cd3d89-4372-488c-b913-96cf93864fc1"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""f0"",
                    ""type"": ""Button"",
                    ""id"": ""df720432-88c0-4772-bad9-2cd406af9ae0"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Restart"",
                    ""type"": ""Button"",
                    ""id"": ""b5d8cfa7-e40d-4f00-a64b-a91abf28835d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Movement"",
                    ""type"": ""Value"",
                    ""id"": ""cfdc84a4-a17b-494c-8f64-21cd2da9c4da"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""9b80b1c4-0adb-4f0e-a29b-6744c792f53b"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e9e36e54-d322-48b8-ad5d-bb82bfc75688"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Interact"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""3D Vector"",
                    ""id"": ""474582c6-6b3b-4b9b-8e27-7fae17889966"",
                    ""path"": ""3DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement3D"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Up"",
                    ""id"": ""d5dc93a9-4643-4a14-ae06-5c4d1eda7b07"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement3D"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Down"",
                    ""id"": ""65ee2b11-ae4a-45c5-aa6a-7e37eafe9983"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement3D"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Left"",
                    ""id"": ""ca491e56-4e61-4669-8a34-b92f2164aee2"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement3D"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Right"",
                    ""id"": ""e8c4ca33-17cb-4833-b1f7-52d71115e2a6"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement3D"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Forward"",
                    ""id"": ""53112242-7be5-44c7-bfce-11fbb824a570"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement3D"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Backward"",
                    ""id"": ""f8e74571-1d7b-4643-9e90-8b7d4877beab"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement3D"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""9c31c779-b3da-4941-b688-287742e9227a"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Run"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9934205d-4b89-4737-8db1-b6ca310c20d6"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Aim"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""fdb16284-c72e-44fc-83cd-43c9c29e4ccc"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Menu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d0c85045-daa9-4a47-8ccb-66d1fb7dc1ba"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Crouch"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""74c8e4ec-6db4-45b7-a58d-dfa043d22e29"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f1"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7afdde68-7c9d-41b2-9dd6-c2488c5f2fb1"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f2"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c1de967a-85df-4b92-9812-7c9ba6d4cebf"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f3"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""697696af-7203-4caf-8369-bd453287591e"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Restart"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""3a7ebe56-c6a7-4854-b3a7-7b977d0876e1"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""5528d460-9b75-4c3a-882e-8b19c0c66b26"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""d6a90297-a336-4a30-9a62-2ee1d338f47a"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""fce343cb-0678-4ff2-8c04-2d5a11f76b95"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""d06cfeb5-14eb-41a8-9618-2b8f6fb23a2a"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""5533611a-2b4d-4b76-89b5-c8376fdee866"",
                    ""path"": ""<Keyboard>/6"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f6"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""fdbc1dbc-a442-4ad5-a7ee-e1a4960a95e6"",
                    ""path"": ""<Keyboard>/4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f4"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""35f804cf-f778-449a-bf6c-a4967a57f5e9"",
                    ""path"": ""<Keyboard>/5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f5"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a2e7b32b-fa20-40bc-9ee4-a809bc8a2432"",
                    ""path"": ""<Keyboard>/7"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f7"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c9e5afb1-d7e2-46cc-a7cf-0eb6d9e0df90"",
                    ""path"": ""<Keyboard>/8"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f8"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e2c63928-da18-4e63-b1d9-1bd25f9ccdd7"",
                    ""path"": ""<Keyboard>/9"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f9"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""79e2eb76-0c85-4c4d-bef6-5360b0e9dbe2"",
                    ""path"": ""<Keyboard>/0"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""f0"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Default
        m_Default = asset.FindActionMap("Default", throwIfNotFound: true);
        m_Default_Jump = m_Default.FindAction("Jump", throwIfNotFound: true);
        m_Default_Interact = m_Default.FindAction("Interact", throwIfNotFound: true);
        m_Default_Movement3D = m_Default.FindAction("Movement3D", throwIfNotFound: true);
        m_Default_Run = m_Default.FindAction("Run", throwIfNotFound: true);
        m_Default_Aim = m_Default.FindAction("Aim", throwIfNotFound: true);
        m_Default_Menu = m_Default.FindAction("Menu", throwIfNotFound: true);
        m_Default_Crouch = m_Default.FindAction("Crouch", throwIfNotFound: true);
        m_Default_f1 = m_Default.FindAction("f1", throwIfNotFound: true);
        m_Default_f2 = m_Default.FindAction("f2", throwIfNotFound: true);
        m_Default_f3 = m_Default.FindAction("f3", throwIfNotFound: true);
        m_Default_f4 = m_Default.FindAction("f4", throwIfNotFound: true);
        m_Default_f5 = m_Default.FindAction("f5", throwIfNotFound: true);
        m_Default_f6 = m_Default.FindAction("f6", throwIfNotFound: true);
        m_Default_f7 = m_Default.FindAction("f7", throwIfNotFound: true);
        m_Default_f8 = m_Default.FindAction("f8", throwIfNotFound: true);
        m_Default_f9 = m_Default.FindAction("f9", throwIfNotFound: true);
        m_Default_f0 = m_Default.FindAction("f0", throwIfNotFound: true);
        m_Default_Restart = m_Default.FindAction("Restart", throwIfNotFound: true);
        m_Default_Movement = m_Default.FindAction("Movement", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Default
    private readonly InputActionMap m_Default;
    private IDefaultActions m_DefaultActionsCallbackInterface;
    private readonly InputAction m_Default_Jump;
    private readonly InputAction m_Default_Interact;
    private readonly InputAction m_Default_Movement3D;
    private readonly InputAction m_Default_Run;
    private readonly InputAction m_Default_Aim;
    private readonly InputAction m_Default_Menu;
    private readonly InputAction m_Default_Crouch;
    private readonly InputAction m_Default_f1;
    private readonly InputAction m_Default_f2;
    private readonly InputAction m_Default_f3;
    private readonly InputAction m_Default_f4;
    private readonly InputAction m_Default_f5;
    private readonly InputAction m_Default_f6;
    private readonly InputAction m_Default_f7;
    private readonly InputAction m_Default_f8;
    private readonly InputAction m_Default_f9;
    private readonly InputAction m_Default_f0;
    private readonly InputAction m_Default_Restart;
    private readonly InputAction m_Default_Movement;
    public struct DefaultActions
    {
        private @DefaultControl m_Wrapper;
        public DefaultActions(@DefaultControl wrapper) { m_Wrapper = wrapper; }
        public InputAction @Jump => m_Wrapper.m_Default_Jump;
        public InputAction @Interact => m_Wrapper.m_Default_Interact;
        public InputAction @Movement3D => m_Wrapper.m_Default_Movement3D;
        public InputAction @Run => m_Wrapper.m_Default_Run;
        public InputAction @Aim => m_Wrapper.m_Default_Aim;
        public InputAction @Menu => m_Wrapper.m_Default_Menu;
        public InputAction @Crouch => m_Wrapper.m_Default_Crouch;
        public InputAction @f1 => m_Wrapper.m_Default_f1;
        public InputAction @f2 => m_Wrapper.m_Default_f2;
        public InputAction @f3 => m_Wrapper.m_Default_f3;
        public InputAction @f4 => m_Wrapper.m_Default_f4;
        public InputAction @f5 => m_Wrapper.m_Default_f5;
        public InputAction @f6 => m_Wrapper.m_Default_f6;
        public InputAction @f7 => m_Wrapper.m_Default_f7;
        public InputAction @f8 => m_Wrapper.m_Default_f8;
        public InputAction @f9 => m_Wrapper.m_Default_f9;
        public InputAction @f0 => m_Wrapper.m_Default_f0;
        public InputAction @Restart => m_Wrapper.m_Default_Restart;
        public InputAction @Movement => m_Wrapper.m_Default_Movement;
        public InputActionMap Get() { return m_Wrapper.m_Default; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(DefaultActions set) { return set.Get(); }
        public void SetCallbacks(IDefaultActions instance)
        {
            if (m_Wrapper.m_DefaultActionsCallbackInterface != null)
            {
                @Jump.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnJump;
                @Jump.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnJump;
                @Jump.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnJump;
                @Interact.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnInteract;
                @Interact.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnInteract;
                @Interact.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnInteract;
                @Movement3D.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMovement3D;
                @Movement3D.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMovement3D;
                @Movement3D.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMovement3D;
                @Run.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRun;
                @Run.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRun;
                @Run.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRun;
                @Aim.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnAim;
                @Aim.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnAim;
                @Aim.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnAim;
                @Menu.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMenu;
                @Menu.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMenu;
                @Menu.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMenu;
                @Crouch.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnCrouch;
                @Crouch.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnCrouch;
                @Crouch.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnCrouch;
                @f1.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF1;
                @f1.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF1;
                @f1.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF1;
                @f2.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF2;
                @f2.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF2;
                @f2.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF2;
                @f3.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF3;
                @f3.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF3;
                @f3.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF3;
                @f4.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF4;
                @f4.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF4;
                @f4.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF4;
                @f5.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF5;
                @f5.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF5;
                @f5.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF5;
                @f6.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF6;
                @f6.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF6;
                @f6.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF6;
                @f7.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF7;
                @f7.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF7;
                @f7.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF7;
                @f8.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF8;
                @f8.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF8;
                @f8.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF8;
                @f9.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF9;
                @f9.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF9;
                @f9.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF9;
                @f0.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF0;
                @f0.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF0;
                @f0.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnF0;
                @Restart.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRestart;
                @Restart.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRestart;
                @Restart.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRestart;
                @Movement.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMovement;
                @Movement.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMovement;
                @Movement.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMovement;
            }
            m_Wrapper.m_DefaultActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Jump.started += instance.OnJump;
                @Jump.performed += instance.OnJump;
                @Jump.canceled += instance.OnJump;
                @Interact.started += instance.OnInteract;
                @Interact.performed += instance.OnInteract;
                @Interact.canceled += instance.OnInteract;
                @Movement3D.started += instance.OnMovement3D;
                @Movement3D.performed += instance.OnMovement3D;
                @Movement3D.canceled += instance.OnMovement3D;
                @Run.started += instance.OnRun;
                @Run.performed += instance.OnRun;
                @Run.canceled += instance.OnRun;
                @Aim.started += instance.OnAim;
                @Aim.performed += instance.OnAim;
                @Aim.canceled += instance.OnAim;
                @Menu.started += instance.OnMenu;
                @Menu.performed += instance.OnMenu;
                @Menu.canceled += instance.OnMenu;
                @Crouch.started += instance.OnCrouch;
                @Crouch.performed += instance.OnCrouch;
                @Crouch.canceled += instance.OnCrouch;
                @f1.started += instance.OnF1;
                @f1.performed += instance.OnF1;
                @f1.canceled += instance.OnF1;
                @f2.started += instance.OnF2;
                @f2.performed += instance.OnF2;
                @f2.canceled += instance.OnF2;
                @f3.started += instance.OnF3;
                @f3.performed += instance.OnF3;
                @f3.canceled += instance.OnF3;
                @f4.started += instance.OnF4;
                @f4.performed += instance.OnF4;
                @f4.canceled += instance.OnF4;
                @f5.started += instance.OnF5;
                @f5.performed += instance.OnF5;
                @f5.canceled += instance.OnF5;
                @f6.started += instance.OnF6;
                @f6.performed += instance.OnF6;
                @f6.canceled += instance.OnF6;
                @f7.started += instance.OnF7;
                @f7.performed += instance.OnF7;
                @f7.canceled += instance.OnF7;
                @f8.started += instance.OnF8;
                @f8.performed += instance.OnF8;
                @f8.canceled += instance.OnF8;
                @f9.started += instance.OnF9;
                @f9.performed += instance.OnF9;
                @f9.canceled += instance.OnF9;
                @f0.started += instance.OnF0;
                @f0.performed += instance.OnF0;
                @f0.canceled += instance.OnF0;
                @Restart.started += instance.OnRestart;
                @Restart.performed += instance.OnRestart;
                @Restart.canceled += instance.OnRestart;
                @Movement.started += instance.OnMovement;
                @Movement.performed += instance.OnMovement;
                @Movement.canceled += instance.OnMovement;
            }
        }
    }
    public DefaultActions @Default => new DefaultActions(this);
    public interface IDefaultActions
    {
        void OnJump(InputAction.CallbackContext context);
        void OnInteract(InputAction.CallbackContext context);
        void OnMovement3D(InputAction.CallbackContext context);
        void OnRun(InputAction.CallbackContext context);
        void OnAim(InputAction.CallbackContext context);
        void OnMenu(InputAction.CallbackContext context);
        void OnCrouch(InputAction.CallbackContext context);
        void OnF1(InputAction.CallbackContext context);
        void OnF2(InputAction.CallbackContext context);
        void OnF3(InputAction.CallbackContext context);
        void OnF4(InputAction.CallbackContext context);
        void OnF5(InputAction.CallbackContext context);
        void OnF6(InputAction.CallbackContext context);
        void OnF7(InputAction.CallbackContext context);
        void OnF8(InputAction.CallbackContext context);
        void OnF9(InputAction.CallbackContext context);
        void OnF0(InputAction.CallbackContext context);
        void OnRestart(InputAction.CallbackContext context);
        void OnMovement(InputAction.CallbackContext context);
    }
}
