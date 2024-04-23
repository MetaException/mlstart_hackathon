import io
import json
import logging
import pydantic
import numpy as np
from typing import List
from fastapi import FastAPI, File, UploadFile, status
from fastapi.encoders import jsonable_encoder
from fastapi.responses import JSONResponse
import torch
from torchvision import transforms
from PIL import Image
from src.datacontract.service_config import ServiceConfig
from src.datacontract.service_output import ServiceOutput
import torchvision.models as models
from scipy.spatial import distance as dist
from ultralytics import YOLO

app = FastAPI()

service_config_path = "./src/configs/service_config.json"
with open(service_config_path, "r") as service_config:
    service_config_json = json.load(service_config)

detector = YOLO(service_config_json["path_to_detector"])


def load_classifier(path_to_pth_weights, device):
    num_classes = 2  # Number of classes
    model = models.resnet152(num_classes=num_classes)

    # Load weights
    model.load_state_dict(torch.load(path_to_pth_weights, map_location=device))

    # Set model to evaluation mode
    model.eval()

    # Move model to specified device
    model.to(device)

    return model


device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
classifier = load_classifier(service_config_json["path_to_classifier"], device)

class_names = ["Lying", "Standing"]

tracked_objects = {}
centroids = {}  # Define the centroids dictionary
object_id_counter = 0
processed_objects = set()


@app.get(
    "/health",
    tags=["healthcheck"],
    summary="Perform health check",
    response_description="Return HTTP status code 200 (OK)",
    status_code=status.HTTP_200_OK,
)
def health_check() -> str:
    return '{"Status" : "OK"}'


@app.get("/reset")
def reset_tracking_variables():
    global tracked_objects, centroids, object_id_counter, processed_objects
    tracked_objects = {}
    centroids = {}
    object_id_counter = 0
    processed_objects = set()


@app.post("/file/", response_model=List[ServiceOutput])
async def inference(image: UploadFile = File(...)):
    global processed_objects
    processed_objects = (
        set()
    )  # Очистка множества обработанных объектов для нового кадра

    # Чтение изображения из загруженного файла
    image_content = await image.read()
    image = Image.open(io.BytesIO(image_content))
    image = image.convert("RGB")
    output_list = []

    # Выполнение обнаружения объектов
    detector_outputs = detector(image)

    # Обработка каждого обнаруженного объекта
    for box in detector_outputs[0].boxes.xyxy:
        box_data = box.tolist()
        xtl, ytl, xbr, ybr = box_data
        object_id, prev_centroid = match_tracked_object(box_data)

        # Если объект отслеживается, обновляем его информацию
        if object_id is not None:
            update_tracked_object(object_id, box_data, prev_centroid)
        else:
            # В противном случае создаем новый отслеживаемый объект
            object_id = create_tracked_object(box_data)

        # Обрезаем объект из изображения и классифицируем его
        crop_object = image.crop((xtl, ytl, xbr, ybr))
        class_name = classify_image(crop_object)

        # Добавляем результат в список
        output_list.append(
            ServiceOutput(
                objectid=object_id,
                classname=class_name,
                xtl=int(xtl),
                xbr=int(xbr),
                ytl=int(ytl),
                ybr=int(ybr),
            )
        )

    return output_list


# Функция для классификации изображения с помощью модели классификации
def classify_image(image):
    transform = transforms.Compose(
        [
            transforms.Resize((224, 224)),
            transforms.ToTensor(),
            transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225]),
        ]
    )
    image_tensor = transform(image).unsqueeze(0).to(device)
    with torch.no_grad():
        output = classifier(image_tensor)
        _, predicted = torch.max(output, 1)
    return class_names[predicted.item()]


# Функция для сопоставления обнаруженного объекта с отслеживаемым объектом на основе расстояния центроидов
def match_tracked_object(box):
    global tracked_objects, centroids, processed_objects
    box_centroid = centroid(box)
    min_distance = float("inf")
    object_id = None
    prev_centroid = None

    for obj_id, obj_centroid in centroids.items():
        if obj_id in processed_objects:
            continue  # Пропускаем объекты, которые уже были обработаны на этом кадре
        distance = dist.euclidean(box_centroid, obj_centroid)
        if distance < min_distance:
            min_distance = distance
            object_id = obj_id
            prev_centroid = obj_centroid

    if object_id is not None and min_distance < 300:
        return object_id, prev_centroid

    return None, None


# Функция для вычисления центроида ограничивающего прямоугольника
def centroid(box):
    x1, y1, x2, y2, *_ = box
    cx = int((x1 + x2) / 2.0)
    cy = int((y1 + y2) / 2.0)
    return (cx, cy)


# Функция для создания нового отслеживаемого объекта
def create_tracked_object(box):
    global tracked_objects, centroids, object_id_counter
    object_id = object_id_counter
    tracked_objects[object_id] = [box]
    centroids[object_id] = centroid(box)
    object_id_counter += 1
    return object_id


# Функция для обновления отслеживаемого объекта
def update_tracked_object(object_id, box, prev_centroid):
    global tracked_objects, centroids, processed_objects
    tracked_objects[object_id].append(box)
    centroids[object_id] = centroid(box)
    processed_objects.add(
        object_id
    )  # Добавление объекта в множество обработанных объектов


# Функция для удаления объектов, которые исчезли
def remove_old_objects():
    global tracked_objects, centroids
    objects_to_remove = [
        object_id for object_id, boxes in tracked_objects.items() if len(boxes) == 0
    ]
    for object_id in objects_to_remove:
        del tracked_objects[object_id]
        del centroids[object_id]
