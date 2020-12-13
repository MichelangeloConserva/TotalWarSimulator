#include "World.h"

#include "btBulletDynamicsCommon.h"
#include <stdio.h>
#include <pybind11/embed.h> // everything needed for embedding
#include <iostream>
#include <Eigen/Dense>  
#include<pybind11/eigen.h>
using Eigen::MatrixXd;
namespace py = pybind11;


int main(int argc, char** argv)
{

	try
	{
		py::scoped_interpreter guard{};


		py::module py_test = py::module::import("pycode.spline_trajectory");

		MatrixXd m(2, 2);
		m(0, 0) = 1;
		m(1, 0) = 2;
		m(0, 1) = 3;
		m(1, 1) = 4;

		py::object result = py_test.attr("get_trajectory")(m);

		MatrixXd res = result.cast<MatrixXd>();
		std::cout << "In c++ \n" << res << std::endl;





		PhysicsEngine::World* physicsEngine = new PhysicsEngine::World();

		physicsEngine->initialize();

		physicsEngine->spawnBox(btVector3(0, -56, 0), btVector3(btScalar(50.), btScalar(50.), btScalar(50.)));
		physicsEngine->spawnSphere(btVector3(2, 10, 0), btScalar(1.));


		for (int i = 0; i < 10; i++)
		{
			physicsEngine->dynamicsWorld->stepSimulation(1.f / 60.f, 10);

			btVector3** positions = physicsEngine->getAllObjectsPosition();
			for (int j = physicsEngine->dynamicsWorld->getNumCollisionObjects() - 1; j >= 0; j--)
			{
				btVector3 curPos = *positions[j];
				printf("world pos object %d = %f,%f,%f\n", j, float(curPos.getX()), float(curPos.getY()), float(curPos.getZ()));
				delete[] positions[j];
			}
			delete[] positions;







		}




		physicsEngine->cleanUp();
		
	}
	catch (std::exception ex)
	{
		std::cout << "ERROR   : " << ex.what() << std::endl;
	}

}

