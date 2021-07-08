using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;

namespace UnityStandardAssets.Characters.FirstPerson {
    [RequireComponent(typeof(CharacterController))]
    public class ArmAgentController : PhysicsRemoteFPSAgentController {
        protected IK_Robot_Arm_Controller getArm() {
            IK_Robot_Arm_Controller arm = GetComponentInChildren<IK_Robot_Arm_Controller>();
            if (arm == null) {
                throw new InvalidOperationException(
                    "Agent does not have kinematic arm or is not enabled.\n" +
                    $"Make sure there is a '{typeof(IK_Robot_Arm_Controller).Name}' component as a child of this agent."
                );
            }
            return arm;
        }


        /*
        This function is identical to `MoveArm` except that rather than
        giving a target position you instead give an "offset" w.r.t.
        the arm's (i.e. wrist's) current location.

        Thus if you want to increase the
        arms x position (in world coordinates) by 0.1m you should
        pass in `offset=Vector3(0.1f, 0f, 0f)` and `coordinateSpace="world"`.
        If you wanted to move the arm 0.1m to the "right" from the agent's
        perspective then you would pass in the same offset but set
        `coordinateSpace="armBase"`. Note that this last movement is **not**
        the same as passing `position=Vector3(0.1f, 0f, 0f)` to the `MoveArm`
        action with `coordinateSpace="wrist"` as, if the wrist has been rotated,
        right need not mean the same thing to the arm base as it does to the wrist.

        Finally note that when `coordinateSpace="wrist"` then both `MoveArm` and
        `MoveArmRelative` are identical.
        */
        public void MoveArmRelative(
            Vector3 offset,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false,
            bool disableRendering = true
        ) {
            IK_Robot_Arm_Controller arm = getArm();
            arm.moveArmRelative(
                controller: this,
                offset: offset,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStart: returnToStart,
                coordinateSpace: coordinateSpace,
                restrictTargetPosition: restrictMovement,
                disableRendering: disableRendering
            );
        }

        public void MoveArm(
            Vector3 position,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false,
            bool disableRendering = true
        ) {
            IK_Robot_Arm_Controller arm = getArm();
            arm.moveArmTarget(
                controller: this,
                target: position,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStart: returnToStart,
                coordinateSpace: coordinateSpace,
                restrictTargetPosition: restrictMovement,
                disableRendering: disableRendering
            );
        }

        // perhaps this should fail if no object is picked up?
        // currently action success happens as long as the arm is
        // enabled because it is a successful "attempt" to pickup something
        public void PickupObject(List<string> objectIdCandidates = null) {
            IK_Robot_Arm_Controller arm = getArm();
            actionFinished(arm.PickupObject(objectIdCandidates, ref errorMessage), errorMessage);
        }

        public override void PickupObject(float x, float y, bool forceAction = false, bool manualInteract = false) {
            throw new InvalidOperationException(
                "You are passing in iTHOR PickupObject parameters (x, y) to the arm agent!"
            );
        }

        public override void PickupObject(string objectId, bool forceAction = false, bool manualInteract = false) {
            throw new InvalidOperationException(
                "You are passing in iTHOR PickupObject parameters (objectId) to the arm agent!"
            );
        }

        public void ReleaseObject() {
            IK_Robot_Arm_Controller arm = getArm();
            arm.DropObject();

            // TODO: only return after object(s) dropped have finished moving
            // currently this will return the frame the object is released
            actionFinished(true);
        }

        // note this does not reposition the center point of the magnet orb
        // so expanding the radius too much will cause it to clip backward into the wrist joint
        public void SetHandSphereRadius(float radius) {
            if (radius < 0.04f || radius > 0.5f) {
                throw new ArgumentOutOfRangeException(
                    $"radius={radius} of hand cannot be less than 0.04m nor greater than 0.5m"
                );
            }

            IK_Robot_Arm_Controller arm = getArm();
            arm.SetHandSphereRadius(radius);
            actionFinished(true);
        }

        public void MoveAgent(
            float ahead = 0,
            float right = 0,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            if (ahead == 0 && right == 0) {
                throw new ArgumentException("Must specify ahead or right!");
            }
            Vector3 direction = new Vector3(x: right, y: 0, z: ahead);
            float fixedDeltaTimeFloat = fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime);

            CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();

            Vector3 directionWorld = transform.TransformDirection(direction);
            Vector3 targetPosition = transform.position + directionWorld;

            collisionListener.Reset();

            IEnumerator move = ContinuousMovement.move(
                controller: this,
                collisionListener: collisionListener,
                moveTransform: this.transform,
                targetPosition: targetPosition,
                fixedDeltaTime: fixedDeltaTimeFloat,
                unitsPerSecond: speed,
                returnToStartPropIfFailed: returnToStart,
                localPosition: false
            );

            if (disableRendering) {
                unrollSimulatePhysics(
                    enumerator: move,
                    fixedDeltaTime: fixedDeltaTimeFloat
                );
            } else {
                StartCoroutine(move);
            }
        }

        public override void MoveAhead(
            float? moveMagnitude = null,
            string objectId = "",                // TODO: Unused, remove when refactoring the controllers
            float maxAgentsDistance = -1f,       // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,            // TODO: Unused, remove when refactoring the controllers
            bool manualInteract = false,         // TODO: Unused, remove when refactoring the controllers
            bool allowAgentsToIntersect = false, // TODO: Unused, remove when refactoring the controllers
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveAgent(
                ahead: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public override void MoveBack(
            float? moveMagnitude = null,
            string objectId = "",                // TODO: Unused, remove when refactoring the controllers
            float maxAgentsDistance = -1f,       // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,            // TODO: Unused, remove when refactoring the controllers
            bool manualInteract = false,         // TODO: Unused, remove when refactoring the controllers
            bool allowAgentsToIntersect = false, // TODO: Unused, remove when refactoring the controllers
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveAgent(
                ahead: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public override void MoveRight(
            float? moveMagnitude = null,
            string objectId = "",                // TODO: Unused, remove when refactoring the controllers
            float maxAgentsDistance = -1f,       // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,            // TODO: Unused, remove when refactoring the controllers
            bool manualInteract = false,         // TODO: Unused, remove when refactoring the controllers
            bool allowAgentsToIntersect = false, // TODO: Unused, remove when refactoring the controllers
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveAgent(
                right: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public override void MoveLeft(
            float? moveMagnitude = null,
            string objectId = "",                // TODO: Unused, remove when refactoring the controllers
            float maxAgentsDistance = -1f,       // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,            // TODO: Unused, remove when refactoring the controllers
            bool manualInteract = false,         // TODO: Unused, remove when refactoring the controllers
            bool allowAgentsToIntersect = false, // TODO: Unused, remove when refactoring the controllers
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveAgent(
                right: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public override void RotateRight(
            float? degrees = null,
            bool manualInteract = false, // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,    // TODO: Unused, remove when refactoring the controllers
            float speed = 1.0f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = true,
            float fixedDeltaTime = 0.02f
        ) {
            RotateAgent(
                degrees: degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                waitForFixedUpdate: waitForFixedUpdate,
                returnToStart: returnToStart,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime
            );
        }

        public override void RotateLeft(
            float? degrees = null,
            bool manualInteract = false, // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,    // TODO: Unused, remove when refactoring the controllers
            float speed = 1.0f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = true,
            float fixedDeltaTime = 0.02f
        ) {
            RotateAgent(
                degrees: -degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                waitForFixedUpdate: waitForFixedUpdate,
                returnToStart: returnToStart,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime
            );
        }

        public void RotateAgent(
            float degrees,
            float speed = 1.0f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = true,
            float fixedDeltaTime = 0.02f
        ) {
            CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();
            collisionListener.Reset();

            // this.transform.Rotate()
            IEnumerator rotate = ContinuousMovement.rotate(
                controller: this,
                collisionListener: this.GetComponentInParent<CollisionListener>(),
                moveTransform: this.transform,
                targetRotation: this.transform.rotation * Quaternion.Euler(0.0f, degrees, 0.0f),
                fixedDeltaTime: disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
                radiansPerSecond: speed,
                returnToStartPropIfFailed: returnToStart
            );

            if (disableRendering) {
                unrollSimulatePhysics(
                    enumerator: rotate,
                    fixedDeltaTime: fixedDeltaTime
                );
            } else {
                StartCoroutine(rotate);
            }
        }

        /*
        Rotates the wrist (in a relative fashion) given some input
        pitch, yaw, and roll offsets. Easiest to see how this works by
        using the editor debugging and shift+alt+(arrow keys or s/w).

        Currently not a completely finished action. New logic is needed
        to prevent self-collisions. In particular we need to
        account for the hierarchy of rigidbodies of each arm joint and
        determine how to detect collision between a given arm joint and other arm joints.
        */
        public void RotateWristRelative(
            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 10f,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            IK_Robot_Arm_Controller arm = getArm();

            arm.rotateWrist(
                controller: this,
                rotation: Quaternion.Euler(pitch, yaw, -roll),
                degreesPerSecond: speed,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart
            );
        }

        /*
        Rotates the elbow (in a relative fashion) by some given
        number of degrees. Easiest to see how this works by
        using the editor debugging and shift+alt+(q/e).

        Currently not a completely finished action. New logic is needed
        to prevent self-collisions. In particular we need to
        account for the hierarchy of rigidbodies of each arm joint and
        determine how to detect collision between a given arm joint and other arm joints.
        */
        public void RotateElbowRelative(
            float degrees,
            float speed = 10f,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            IK_Robot_Arm_Controller arm = getArm();

            arm.rotateElbowRelative(
                controller: this,
                degrees: degrees,
                degreesPerSecond: speed,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart
            );
        }

        /*
        Same as RotateElbowRelative but rotates the elbow to a given angle directly.
        */
        public void RotateElbow(
            float degrees,
            float speed = 10f,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            IK_Robot_Arm_Controller arm = getArm();

            arm.rotateElbow(
                controller: this,
                degrees: degrees,
                degreesPerSecond: speed,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart
            );
        }


        // constrain arm's y position based on the agent's current capsule collider center and extents
        // valid Y height from action.y is [0, 1.0] to represent the relative min and max heights of the
        // arm constrained by the agent's capsule
        public void MoveArmBase(
            float y,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            if (y < 0 || y > 1) {
                throw new ArgumentOutOfRangeException($"y={y} value must be [0, 1.0].");
            }

            IK_Robot_Arm_Controller arm = getArm();
            arm.moveArmBase(
                controller: this,
                height: y,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart,
                disableRendering: disableRendering
            );
        }

#if UNITY_EDITOR
        // debug for static arm collisions from collision listener
        public void GetMidLevelArmCollisions() {
            IK_Robot_Arm_Controller arm = getArm();
            CollisionListener collisionListener = arm.GetComponentInChildren<CollisionListener>();
            if (collisionListener != null) {
                List<Dictionary<string, string>> collisions = new List<Dictionary<string, string>>();
                foreach (var sc in collisionListener.StaticCollisions()) {
                    Dictionary<string, string> element = new Dictionary<string, string>();
                    if (sc.simObjPhysics != null) {
                        element["objectType"] = "simObjPhysics";
                        element["name"] = sc.simObjPhysics.objectID;
                    } else {
                        element["objectType"] = "gameObject";
                        element["name"] = sc.gameObject.name;
                    }
                    collisions.Add(element);
                }
                actionFinished(true, collisions);
            }
        }

        // debug for static arm collisions from collision listener
        public void DebugMidLevelArmCollisions() {
            IK_Robot_Arm_Controller arm = getArm();
            List<CollisionListener.StaticCollision> scs = arm.collisionListener.StaticCollisions();
            Debug.Log("Total current active static arm collisions: " + scs.Count);
            foreach (CollisionListener.StaticCollision sc in scs) {
                Debug.Log("Arm static collision: " + sc.name);
            }
            actionFinished(true);
        }
#endif
    }
}
