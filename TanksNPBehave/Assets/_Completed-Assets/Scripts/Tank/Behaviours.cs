using UnityEngine;
using NPBehave;
using System.Collections.Generic;

namespace Complete
{
    /*
    Example behaviour trees for the Tank AI.  This is partial definition:
    the core AI code is defined in TankAI.cs.

    Use this file to specifiy your new behaviour tree.
     */
    public partial class TankAI : MonoBehaviour
    {
        private Root CreateBehaviourTree() {

            switch (m_Behaviour) {
				case 0:
					return FunBehaviour();
				case 1:
					return DeadlyBehaviour();
				case 2:
					return FrightenedBehaviour();
				case 3:
					return UnpredictableBehaviour();

                default:
                    return new Root (new Action(()=> Turn(0.1f)));
            }
        }

        /* Actions */

        private Node StopTurning() {
            return new Action(() => Turn(0));
        }

        private Node RandomFire() {
            return new Action(() => Fire(UnityEngine.Random.Range(0.0f, 1.0f)));
        }



        /* Example behaviour trees */

        // Constantly spin and fire on the spot 
        private Root SpinBehaviour(float turn, float shoot) {
            return new Root(new Sequence(
                        new Action(() => Turn(turn)),
                        new Action(() => Fire(shoot))
                    ));
        }

        // Turn to face your opponent and fire
        private Root TrackBehaviour() {
            return new Root(
                new Service(0.2f, UpdatePerception,
                    new Selector(
                        new BlackboardCondition("targetOffCentre",
                                                Operator.IS_SMALLER_OR_EQUAL, 0.1f,
                                                Stops.IMMEDIATE_RESTART,
                            // Stop turning and fire
                            new Sequence(StopTurning(),
                                        new Wait(2f),
                                        RandomFire())),
                        new BlackboardCondition("targetOnRight",
                                                Operator.IS_EQUAL, true,
                                                Stops.IMMEDIATE_RESTART,
                            // Turn right toward target
                            new Action(() => Turn(0.2f))),
                            // Turn left toward target
                            new Action(() => Turn(-0.2f))
                    )
                )
            );
        }








		//Actions are stored as node functions so they can be reused in different behaviours 
		public Node TargetUnit () {
			//Updates the perception values every 0.1 seconds, run Selector 
			return new Service (0.1f, UpdatePerception, 
				//Splits the node, if target is to right, than Turn (0.3f) succeeds in running, and the tank turns right
				//Otherwise tank turns left 
				new Selector (
					new BlackboardCondition ("targetOnRight",
						Operator.IS_EQUAL, true,
						Stops.NONE,
						// Turn right toward target
						new Action (() => Turn (0.3f))),
					new Action (() => Turn (-0.3f))
				)
			);
		}
		public Node FireAndTurn (float fireVel = 0f) {
			return new Sequence(
				new Action(() => Turn(1f)),		//Sets rotation rate 
				new Action(() => Fire(fireVel)) //Set shooting velocity at 0 for maximum fire rate
			);
		}
		public Node DeadlyFireAndTurn () {
			return new Sequence(
				//(Using Mathf.Sin for movement and rotation allows for left and right, front and back movement in case tank gets stuck) 
				new Action(() => Move(Mathf.Sin (Time.time * 2f))),	//Sets Movement rate
				new Action(() => Turn(Mathf.Sin (Time.time))),	//Sets rotation rate 
				new Action(() => Fire(0f)) 						//Sets shooting velocity at 0 to maximum fire rate
			);
		}
		public Node RetreatAndFire (float fireVel = 0.5f) {
			return new Sequence (
					//(using Mathf.Sin (-0.6) means that the tank will mostly move backwards, but a bit forwards in case it's stuck)
				new Action (() => Move (Mathf.Sin (Time.time) - 0.6f)),	
				new Action (() => Fire (fireVel)), 						//Sets shooting velocity at 0 to maximum fire rate

				TargetUnit ()
			);
		}
		public Node SeekAndFire (float fireVel = 0.2f) {
			return new Sequence (
				//(using Mathf.Sin (-0.6) means that the tank will mostly move backwards, but a bit forwards in case it's stuck)
				new Action (() => Move (1f)),	
				new Action (() => Fire (fireVel)), 						//Sets shooting velocity at 0 to maximum fire rate

				TargetUnit ()
			);
		}




		private Root FunBehaviour() {
			return new Root(FireAndTurn ());
		}
		private Root DeadlyBehaviour() {
			return new Root(DeadlyFireAndTurn ());
		}
		private Root FrightenedBehaviour() {
			return new Root(RetreatAndFire ());
		}
		private Root UnpredictableBehaviour() {
			//Modulars time to loop actions throughtout 20 second intervals
			//If modular time is < 4, FireAndTurn
			//else If modular time is > 16, DeadlyFireAndTurn
			//else If modular time is < 10, RetreatAndFire
			//else If modular time is < 5, SeekAndFire
			return new Root(
				//Selector used so only 1 node is excuted at a time
				new Selector (
					new Condition (() => {return (Time.time % 20 < 4f);}, Stops.IMMEDIATE_RESTART, FireAndTurn ()),
					new Condition (() => {return (Time.time % 20 > 16);}, Stops.IMMEDIATE_RESTART, DeadlyFireAndTurn ()),
					new Condition (() => {return (Time.time % 20 > 10f);}, Stops.IMMEDIATE_RESTART, RetreatAndFire ()),
					new Condition (() => {return (Time.time % 20 > 5f);}, Stops.IMMEDIATE_RESTART, SeekAndFire ()),
					TargetUnit ()
				)
			);

		}



//		private Root TurnTowardsNearestTank(float turn, float shoot) {
//			int playerLayer = LayerMask.GetMask ("Players");
//			Collider [] colls = Physics.OverlapSphere (transform.position, 1000f, playerLayer);
//
//			Collider targetColl = null;
//
//			for (int i = 0; i < colls.Length; i++) {
//				if (colls [i].gameObject != gameObject) {
//					targetColl = colls [i];
//				}
//			}
//			if (targetColl) {
//				Quaternion rotationAxis = Quaternion.AngleAxis (1f, transform.up);
//				return new Root (new Sequence (
//					new Action (() => Turn (turn)),
//					new Action (() => Fire (shoot))
//				));
//
//			} else {
//				return SpinBehaviour(-0.05f, 1f);
//			}
//		}

        private void UpdatePerception() {
            Vector3 targetPos = TargetTransform().position;
            Vector3 localPos = this.transform.InverseTransformPoint(targetPos);
            Vector3 heading = localPos.normalized;
            blackboard["targetDistance"] = localPos.magnitude;
            blackboard["targetInFront"] = heading.z > 0;
            blackboard["targetOnRight"] = heading.x > 0;
            blackboard["targetOffCentre"] = Mathf.Abs(heading.x);
        }

    }
}