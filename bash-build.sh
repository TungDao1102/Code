#!/bin/bash

read -p "Enter project do you want build (backend[1] | frontend[2] | all[3]) or (clear docker[0]): " OPTION

WORK_PATH="/home/folder-name"
GITHUB_AUTH="username:access-token"

function build_backend() 
{
  cd "${WORK_PATH}/source-backend/"
  
  git checkout .

  git pull "https://${GITHUB_AUTH}@git-repository.git"

  dotnet publish "${WORK_PATH}/source-backend/src/Web.API/Web.API.csproj" -o "${WORK_PATH}/backend/"

  sudo systemctl restart backend.service
}

function build_frontend() 
{
  cd "${WORK_PATH}/frontend/"

  git checkout .

  git pull "https://${GITHUB_AUTH}@git-repository.git"

  docker-compose build

  docker-compose up -d
}

function clear_docker_image()
{
  docker image ls
  
  docker system prune --all --force
}

if [ "${OPTION}" == "clear" ] | [ "${OPTION}" == "0" ];
then

  clear_docker_image
  
elif [ "${OPTION}" == "backend" ] | [ "${OPTION}" == "1" ];
then

  build_backend

elif [ "${OPTION}" == "frontend" ] | [ "${OPTION}" == "2" ];
then

  build_frontend
  
elif [ "${OPTION}" == "all" ] | [ "${OPTION}" == "3" ];
then

  build_backend
  build_frontend

else

  echo "Wrong command."

fi
