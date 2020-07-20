import numpy as np
import pymunk
import math

from pymunk.vec2d import Vec2d
from scipy.spatial.transform import Rotation as R


def rotate_matrix(M, angle): 
    assert type(M) == np.ndarray and M.shape[1] == 3, "M must be a np.array of Nx3"
    return R.from_euler('z', angle).apply(M)[:,:-1]

def spaced_vector(n, size, dist):
    dd = n * size + (n-1)*dist - size
    return np.linspace(-dd/2, dd/2, n)

def vector_clipping(v, min_length, max_length):
    if v.length > max_length:
        return v.normalized() * max_length
    elif v.length < min_length:
        return v.normalized() * min_length        
    return v

def vect_linspace(v1, v2, n):
    prs = np.linspace(0,1, n+1).tolist()            
    res = []
    for i in range(n):
        pr_start = prs[i]
        pr_end = prs[i+1]
        cur_start = v1*(pr_start) + (v2)*(1-pr_start)
        cur_end = v1*(pr_end) + (v2)*(1-pr_end)
        res.append((cur_start, cur_end))
    return res
        
def vect_middles_of_linspace(v1,v2,n):
    divs = np.linspace(0,1, n+2).tolist()[1:-1]
    return np.array([(v1 * div + v2 * (1 - div)).int_tuple for div in divs])

def calc_vertices(pos, h, w, angle):
    vs = []  
    vs.append(list(Vec2d( h/2, w/2))+[0])                
    vs.append(list(Vec2d( h/2,-w/2))+[0])                
    vs.append(list(Vec2d(-h/2,-w/2))+[0])                
    vs.append(list(Vec2d(-h/2, w/2))+[0])      

    M = np.array(vs)
    MR = R.from_euler('z', angle).apply(M)[:,:-1].tolist()

    return [pos + Vec2d(v) for v in MR]

def get_BB(v1, v2):
    v1,v2 = Vec2d(v1), Vec2d(v2)
    left = min(v1.x, v2.x) #+ abs(v1.x - v2.x) / 2
    bot  = min(v1.y, v2.y) #- abs(v1.y - v2.y) / 2
    right = max(v1.x, v2.x) #+ abs(v1.x - v2.x) / 2
    top  = max(v1.y, v2.y) #- abs(v1.y - v2.y) / 2
    return pymunk.BB(left, bot, right, top)

def dist(p1, p2, c): 
    x1,y1 = p1
    x2,y2 = p2
    x3,y3 = c
    px = x2-x1
    py = y2-y1

    something = px*px + py*py

    u =  ((x3 - x1) * px + (y3 - y1) * py) / float(something)

    if u > 1:
        u = 1
    elif u < 0:
        u = 0

    x = x1 + u * px
    y = y1 + u * py

    dx = x - x3
    dy = y - y3

    dist = math.sqrt(dx*dx + dy*dy)

    return dist

def is_rect_circle_collision(body, center, radius):
    vertices = body.vertices
    if body.contains_vect(center): return True
    
    for i, j in zip([0, 1, 2, 3], [1, 2, 3, 0]):
        if dist(vertices[i], vertices[j], center) < radius: return True
    
    return False

def do_polygons_intersect(a, b):
    """
    

    Parameters
    ----------
    a : TYPE
        list of vertices.
    b : TYPE
        list of vertices.

    Returns
    -------
    True is intersects False otherwise

    """    

    polygons = [a, b];
    minA, maxA, projected, i, i1, j, minB, maxB = None, None, None, None, None, None, None, None

    for i in range(len(polygons)):

        # for each polygon, look at each edge of the polygon, and determine if it separates
        # the two shapes
        polygon = polygons[i];
        for i1 in range(len(polygon)):

            # grab 2 vertices to create an edge
            i2 = (i1 + 1) % len(polygon);
            p1 = polygon[i1];
            p2 = polygon[i2];

            # find the line perpendicular to this edge
            normal = { 'x': p2[1] - p1[1], 'y': p1[0] - p2[0] };

            minA, maxA = None, None
            # for each vertex in the first shape, project it onto the line perpendicular to the edge
            # and keep track of the min and max of these values
            for j in range(len(a)):
                projected = normal['x'] * a[j][0] + normal['y'] * a[j][1];
                if (minA is None) or (projected < minA): 
                    minA = projected

                if (maxA is None) or (projected > maxA):
                    maxA = projected

            # for each vertex in the second shape, project it onto the line perpendicular to the edge
            # and keep track of the min and max of these values
            minB, maxB = None, None
            for j in range(len(b)): 
                projected = normal['x'] * b[j][0] + normal['y'] * b[j][1]
                if (minB is None) or (projected < minB):
                    minB = projected

                if (maxB is None) or (projected > maxB):
                    maxB = projected

            # if there is no overlap between the projects, the edge we are looking at separates the two
            # polygons, and we know there is no overlap
            if (maxA < minB) or (maxB < minA):
                return False;
    return True




