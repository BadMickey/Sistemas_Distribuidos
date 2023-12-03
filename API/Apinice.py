from flask import Flask, request, jsonify
import json
import socket

api = Flask(__name__)

# Dados que você quer enviar
'''  padrão adicionar bloco
  {
        "Address": "quarto",
        "SensorId": 10
    }'''

@api.route('/adicionar_bloco', methods=['POST'])
def receber_json():
    json_data = request.json  # Obtém o JSON enviado na requisição POST
    json_string = json.dumps(json_data)
    host = '10.4.6.30'  # IP do host
    port = 13000  # P..orta utilizada pelo servidor

    bytes_data = ("ADD_BLOCK_API:" + json_string).encode('utf-8')

    # Criação do socket TCP
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    confirmation_message = None


    try:
        # Conectando ao servidor
        client_socket.connect((host, port))

        # Enviando os dados
        client_socket.sendall(bytes_data)
        
        print("Dados enviados com sucesso para a Blockchain!")

        # Recebendo a mensagem de confirmação
        confirmation_message = client_socket.recv(1024).decode('utf-8')
        if confirmation_message:
            return jsonify({"message": confirmation_message}), 200
        else:
            return jsonify({"error": "Não foi recebida mensagem da Blockchain"}), 500
    except Exception as e:
        print(f"Erro ao enviar dados: {e}")
        return jsonify({"error": f"Erro ao enviar dados: {e}"}), 500
    finally:
        if confirmation_message:
            # Fechando o socket
            client_socket.close()

@api.route('/alterar_status', methods=['PUT'])
def alterar_status():
    json_data = request.json  # Obtém o JSON enviado na requisição POST
    json_string = json.dumps(json_data)
    host = '10.4.6.30'  # IP do host
    port = 13000  # P..orta utilizada pelo servidor

    bytes_data = ("CHANGE_STATUS_API:" + json_string).encode('utf-8')

    # Criação do socket TCP
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    confirmation_message = None

    try:
        # Conectando ao servidor
        client_socket.connect((host, port))

        # Enviando os dados
        client_socket.sendall(bytes_data)
        
        print("Dados enviados com sucesso para a Blockchain!")

        confirmation_message = client_socket.recv(1024).decode('utf-8')
        if confirmation_message:
            return jsonify({"message": confirmation_message}), 200
        else:
            return jsonify({"error": "Não foi recebida mensagem da Blockchain"}), 500
    except Exception as e:
        print(f"Erro ao enviar dados: {e}")
        return jsonify({"error": f"Erro ao enviar dados: {e}"}), 500
    finally:
       if confirmation_message:
            # Fechando o socket
            client_socket.close()
'''  padrão alterar status
    {
        "SensorId": 50,
        "MotionDetected": false
    }'''
@api.route('/verificar_blockchain', methods=['GET'])
def verificar_blockchain():
    host = '10.4.6.30'  # IP do host
    port = 13000  # P..orta utilizada pelo servidor

    bytes_data = ("VERIFY_CHAIN_API:").encode('utf-8')

    # Criação do socket TCP
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    confirmation_message = None

    try:
        # Conectando ao servidor
        client_socket.connect((host, port))

        # Enviando os dados
        client_socket.sendall(bytes_data)
        
        print("Requisição enviada com sucesso para a Blockchain!")

        confirmation_message = client_socket.recv(1024).decode('utf-8')
        if confirmation_message:
            return jsonify({"message": confirmation_message}), 200
        else:
            return jsonify({"error": "Não foi recebida mensagem da Blockchain"}), 500
    except Exception as e:
        print(f"Erro ao enviar dados: {e}")
        return jsonify({"error": f"Erro ao enviar dados: {e}"}), 500
    finally:
       if confirmation_message is not None:
            # Fechando o socket
            client_socket.close()

@api.route('/verify_status', methods=['GET'])
def verificar_status():
    json_data = request.json  # Obtém o JSON enviado na requisição POST
    json_string = json.dumps(json_data)
    host = '10.4.6.30'  # IP do host
    port = 13000  # P..orta utilizada pelo servidor

    bytes_data = ("VERIFY_STATUS_API:" + json_string).encode('utf-8')

    # Criação do socket TCP
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    confirmation_message = None

    try:
        # Conectando ao servidor
        client_socket.connect((host, port))

        # Enviando os dados
        client_socket.sendall(bytes_data)
        
        print("Requisição enviada com sucesso para a Blockchain!")

        confirmation_message = client_socket.recv(1024).decode('utf-8')
        if confirmation_message:
            return jsonify({"message": confirmation_message}), 200
        else:
            return jsonify({"error": "Não foi recebida mensagem da Blockchain"}), 500
    except Exception as e:
        print(f"Erro ao enviar dados: {e}")
        return jsonify({"error": f"Erro ao enviar dados: {e}"}), 500
    finally:
       if confirmation_message is not None:
            # Fechando o socket
            client_socket.close()
'''  padrão verificar status
    {
        "SensorId": 50
    }'''

    
# run the API
if __name__ == '__main__':
    api.run(port=10000, host='localhost', debug=True)