import numpy as np

def test_mat(m):
    print ("Inside python m = \n ",m )
    m[0,0] = 10
    m[1,1] = np.exp(2)
    return m