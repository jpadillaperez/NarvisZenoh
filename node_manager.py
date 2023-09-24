import json
import uuid
import zenoh

from zenoh import Sample

#from dataclasses import dataclass
#from pycdr2 import IdlStruct
#from pycdr2.types import int8, int32, uint32, float64

    

class Position:
    def __init__(self, x=0, y=0, z=0):
        self.x = x
        self.y = y
        self.z = z


class Gameobject:
    def __init__(self):
        self.position = Position()
        self.isLockedBy = ""

    def setPosition(self, x, y, z):
        self.position = Position(x, y, z)


class receivedMessage:
    #structure of the message sent to node manager
    def __init__(self, Obj_id, User_id, Function, Position):
        self.Obj_id = Obj_id
        self.User_id = User_id
        self.Function = Function
        self.Position = Position


class sentMessage:
    #structure of the message received by each user
    def __init__(self, Obj_id="", Position=[0, 0, 0], Active=True, User_id=""):
        self.User_id = User_id
        self.Obj_id = Obj_id
        self.Position = Position
        self.Active = Active


Sphere = Gameobject()
Sphere.position = Position(-1.5, 0, 5)
sphereUID = "Sphere"

Square = Gameobject()
Square.position = Position(1.5, 0, 5)
squareUID = "Square"

objects = {sphereUID: Sphere, squareUID: Square}


class node_manager():
    def __init__(self):
        conf = zenoh.Config()

        conf.insert_json5(zenoh.config.MODE_KEY, json.dumps("peer"))

        print("Openning session...")
        self.session = zenoh.open(conf)

        zenoh.init_logger()

        self.pub_topic = "ManagerRepliesStrings"
        self.sub_topic = "UserActionsStrings"

        #ros
        #self.subscription_ = self.create_subscription(String, "UserReports", self.applyFunctionality, 10)
        #self.publisher_ = self.create_publisher(String, 'ManagerNodeCommands', 10)

        #zenoh
        self.sub = self.session.declare_subscriber(self.sub_topic, self.applyFunctionality)


        self.commands = {'ChangePosition': self.ChangePosition, 'GrabObject': self.GrabObject, 'ReleaseObject': self.ReleaseObject,
                         'CreateObject': self.CreateObject, 'DeleteObject': self.DeleteObject, "UserJoin": self.UserJoin}

    def applyFunctionality(self, msg):
        #print("Received Message!")
        print("Received Message: " + str(msg.value.payload))
        msg = receivedMessage(**json.loads(msg.value.payload))
        func = self.commands[msg.Function]
        func(msg.Obj_id, msg.User_id, msg.Position)

    def ChangePosition(self, object_id, user_id, pose):
        if (objects[object_id].isLockedBy == user_id):
            objects[object_id].position.x = pose[0]
            objects[object_id].position.y = pose[1]
            print("Position Changed")
            msg = self.GenerateMessage(object_id)

            #print("Sending message: " + msg)

            #zenoh
            self.session.put(self.pub_topic, msg)
            #ros
            #self.publisher_.publish(msg)

    def GrabObject(self, object_id, user_id, _):
        if (objects[object_id].isLockedBy == ""):
            objects[object_id].isLockedBy = user_id

    def ReleaseObject(self, object_id, user_id, _):
        if (objects[object_id].isLockedBy == user_id):
            objects[object_id].isLockedBy = ""


    def CreateObject(self, _, user_id, position):
        #add object to dictionary with position
        object_id = str(uuid.uuid4())
        tempObj = Gameobject()
        tempObj.position = Position(position[0], position[1], 5)
        objects[object_id] = tempObj
        objects[object_id].isLockedBy = user_id
        
        #print("Created object. Sending message: Obj_id: ", object_id, " Position: ", objects[object_id].position.x,
        #        objects[object_id].position.y, objects[object_id].position.z, " Active: ", objects[object_id].isLockedBy, " User_id: ", user_id)
        
        msg = self.GenerateMessage(object_id)
        
        #zenoh
        self.session.put(self.pub_topic, msg)


    def DeleteObject(self, object_id, user_id, _):
        #delete object from dictionary with position
        msg = sentMessage(object_id, [objects[object_id].position.x,
                          objects[object_id].position.y, objects[object_id].position.z], False)
        
        msg = self.RefineMessage(msg=msg)

        #zenoh
        self.session.put(self.pub_topic, json.dumps(
            msg, default=lambda o: o.__dict__, sort_keys=False, indent=None))
        
        #ros
        #self.publisher_.publish(json.dumps(msg, default=lambda o: o.__dict__, sort_keys=True, indent=4))

        del objects[object_id]


    def UserJoin(self, object_id, user_id, _):
        #add user
        user_id = str(uuid.uuid4())
        msg = sentMessage(User_id=user_id)

        msg = self.RefineMessage(msg=msg)
        
        #zenoh
        self.session.put(self.pub_topic, json.dumps(
            msg, default=lambda o: o.__dict__, sort_keys=False, indent=None))
        #ros
        #self.publisher_.publish(json.dumps(msg, default=lambda o: o.__dict__, sort_keys=True, indent=4))


    def GenerateMessage(self, Obj_id):
        msg = sentMessage(Obj_id= Obj_id, 
                          Position= [objects[Obj_id].position.x, objects[Obj_id].position.y, objects[Obj_id].position.z], 
                          User_id= objects[Obj_id].isLockedBy)
        
        msg = self.RefineMessage(msg=msg)

        return msg
    

    def RefineMessage(self, msg):
        #msg = "b'" + json.dumps(msg, default=lambda o: o.__dict__, sort_keys=False, indent=None) + "'"
        msg = json.dumps(msg, default=lambda o: o.__dict__,
                         sort_keys=False, indent=None)

        print("Sent message: " + msg)
        return msg
    

    def quit(self):
        self.session.close()


def main(args=None):
    nm = node_manager()
    #when press q or ctrl+c quit the program
    while True:
        if input() == 'q':
            nm.quit()
            break


if __name__ == '__main__':
    main()
