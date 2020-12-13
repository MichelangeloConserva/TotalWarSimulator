#pragma once

#include "btBulletDynamicsCommon.h"


namespace PhysicsEngine
{
	class World
	{
	public:
		btAlignedObjectArray<btCollisionShape*> collisionShapes;
		btDiscreteDynamicsWorld* dynamicsWorld;
		btSequentialImpulseConstraintSolver* solver;
		btBroadphaseInterface* overlappingPairCache;
		btCollisionDispatcher* dispatcher;
		btDefaultCollisionConfiguration* collisionConfiguration;


		void initialize();
		void spawnBox(btVector3 position, btVector3 dimensions);
		void spawnSphere(btVector3 position, btScalar radius);

		btVector3** getAllObjectsPosition();

		void step();
		void cleanUp();


	};
}



